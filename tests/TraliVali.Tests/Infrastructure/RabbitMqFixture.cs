using Microsoft.Extensions.Logging;
using Testcontainers.RabbitMq;
using TraliVali.Infrastructure.Messaging;

namespace TraliVali.Tests.Infrastructure;

/// <summary>
/// Test fixture for RabbitMQ using Testcontainers
/// </summary>
public class RabbitMqFixture : IAsyncLifetime
{
    private RabbitMqContainer? _rabbitMqContainer;

    /// <summary>
    /// Gets the RabbitMQ service for testing
    /// </summary>
    public RabbitMqService Service { get; private set; } = null!;

    /// <summary>
    /// Gets the RabbitMQ connection string
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the RabbitMQ configuration
    /// </summary>
    public TraliVali.Infrastructure.Messaging.RabbitMqConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    /// Initializes the fixture by starting the RabbitMQ container
    /// </summary>
    public async Task InitializeAsync()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.12-management-alpine")
            .Build();

        await _rabbitMqContainer.StartAsync();

        ConnectionString = _rabbitMqContainer.GetConnectionString();
        
        // Parse the connection string to create configuration
        var uri = new Uri(ConnectionString);
        Configuration = new TraliVali.Infrastructure.Messaging.RabbitMqConfiguration
        {
            HostName = uri.Host,
            Port = uri.Port,
            UserName = uri.UserInfo.Split(':')[0],
            Password = uri.UserInfo.Split(':')[1],
            VirtualHost = string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/" ? "/" : uri.AbsolutePath.TrimStart('/')
        };

        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<RabbitMqService>();
        
        Service = new RabbitMqService(Configuration, logger);
        await Service.InitializeAsync();
    }

    /// <summary>
    /// Cleans up the fixture by stopping the RabbitMQ container
    /// </summary>
    public async Task DisposeAsync()
    {
        Service?.Dispose();

        if (_rabbitMqContainer != null)
        {
            await _rabbitMqContainer.StopAsync();
            await _rabbitMqContainer.DisposeAsync();
        }
    }
}
