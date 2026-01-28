namespace TraliVali.Infrastructure.Messaging;

/// <summary>
/// Interface for publishing messages to RabbitMQ
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the specified routing key
    /// </summary>
    /// <param name="routingKey">The routing key for the message</param>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync(string routingKey, string message, CancellationToken cancellationToken = default);
}
