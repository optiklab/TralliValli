using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace TraliVali.Infrastructure.Messaging;

/// <summary>
/// Configuration for RabbitMQ service
/// </summary>
public class RabbitMqConfiguration
{
    /// <summary>
    /// Gets or sets the RabbitMQ host name
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the RabbitMQ port
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the RabbitMQ username
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the RabbitMQ password
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";
}

/// <summary>
/// RabbitMQ service implementation with topic exchange and retry policies
/// </summary>
public class RabbitMqService : IMessagePublisher, IMessageConsumer, IDisposable
{
    private const string ExchangeName = "tralivali.messages";
    private const string ExchangeType = "topic";

    private static readonly string[] QueueNames = new[]
    {
        "messages.process",
        "files.process",
        "archival.process",
        "backup.process"
    };

    private readonly RabbitMqConfiguration _configuration;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly Dictionary<string, string> _consumerTags = new();
    private readonly object _channelLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqService"/> class
    /// </summary>
    /// <param name="configuration">The RabbitMQ configuration</param>
    /// <param name="logger">The logger instance</param>
    public RabbitMqService(RabbitMqConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure retry policy with Polly
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>()
                    .Handle<SocketException>()
                    .Handle<IOException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "RabbitMQ operation failed, retrying... Attempt {AttemptNumber}",
                        args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Initializes the RabbitMQ connection, exchange, and queues
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
                return;

            await _resiliencePipeline.ExecuteAsync(async token =>
            {
                _logger.LogInformation("Initializing RabbitMQ connection to {HostName}:{Port}", 
                    _configuration.HostName, _configuration.Port);

                var factory = new ConnectionFactory
                {
                    HostName = _configuration.HostName,
                    Port = _configuration.Port,
                    UserName = _configuration.UserName,
                    Password = _configuration.Password,
                    VirtualHost = _configuration.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare exchange
                _channel.ExchangeDeclare(
                    exchange: ExchangeName,
                    type: ExchangeType,
                    durable: true,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation("Created exchange: {ExchangeName}", ExchangeName);

                // Declare queues and bind them to the exchange
                foreach (var queueName in QueueNames)
                {
                    _channel.QueueDeclare(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    // Bind queue to exchange with routing key matching the queue name
                    _channel.QueueBind(
                        queue: queueName,
                        exchange: ExchangeName,
                        routingKey: queueName,
                        arguments: null);

                    _logger.LogInformation("Created and bound queue: {QueueName}", queueName);
                }

                _isInitialized = true;
                _logger.LogInformation("RabbitMQ initialization completed successfully");

                return ValueTask.CompletedTask;
            }, cancellationToken);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task PublishAsync(string routingKey, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(routingKey))
            throw new ArgumentNullException(nameof(routingKey));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message));

        await EnsureInitializedAsync(cancellationToken);

        await _resiliencePipeline.ExecuteAsync(async token =>
        {
            lock (_channelLock)
            {
                var body = Encoding.UTF8.GetBytes(message);
                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel!.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug("Published message to {RoutingKey}", routingKey);
            }
            return ValueTask.CompletedTask;
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task StartConsumingAsync(string queueName, Func<string, Task> onMessageReceived, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentNullException(nameof(queueName));
        if (onMessageReceived == null)
            throw new ArgumentNullException(nameof(onMessageReceived));

        await EnsureInitializedAsync(cancellationToken);

        lock (_channelLock)
        {
            if (_channel == null)
                throw new InvalidOperationException("Channel is not initialized");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger.LogDebug("Received message from {QueueName}", queueName);

                    await onMessageReceived(message);

                    lock (_channelLock)
                    {
                        _channel?.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from {QueueName}", queueName);
                    // Reject and requeue the message
                    lock (_channelLock)
                    {
                        try
                        {
                            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                        }
                        catch (Exception nackEx)
                        {
                            _logger.LogError(nackEx, "Error sending NACK for message from {QueueName}", queueName);
                        }
                    }
                }
            };

            var consumerTag = _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _consumerTags[queueName] = consumerTag;

            _logger.LogInformation("Started consuming from queue: {QueueName}", queueName);
        }
    }

    /// <inheritdoc/>
    public void StopConsuming()
    {
        lock (_channelLock)
        {
            if (_consumerTags.Count > 0 && _channel != null)
            {
                foreach (var consumerTag in _consumerTags.Values)
                {
                    try
                    {
                        _channel.BasicCancel(consumerTag);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cancelling consumer {ConsumerTag}", consumerTag);
                    }
                }
                _consumerTags.Clear();
                _logger.LogInformation("Stopped consuming messages");
            }
        }
    }

    /// <summary>
    /// Gets the list of queue names managed by this service
    /// </summary>
    public static IReadOnlyList<string> GetQueueNames() => QueueNames;

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Disposes the RabbitMQ resources
    /// </summary>
    public void Dispose()
    {
        StopConsuming();
        
        if (_channel != null)
        {
            _channel.Close();
            _channel.Dispose();
            _channel = null;
        }

        if (_connection != null)
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }

        _initializationLock.Dispose();
    }
}
