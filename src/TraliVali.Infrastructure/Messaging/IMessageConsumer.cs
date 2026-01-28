namespace TraliVali.Infrastructure.Messaging;

/// <summary>
/// Interface for consuming messages from RabbitMQ
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Starts consuming messages from the specified queue
    /// </summary>
    /// <param name="queueName">The name of the queue to consume from</param>
    /// <param name="onMessageReceived">Callback to handle received messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartConsumingAsync(string queueName, Func<string, Task> onMessageReceived, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops consuming messages
    /// </summary>
    void StopConsuming();
}
