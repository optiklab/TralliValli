using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TraliVali.Messaging;

/// <summary>
/// Extension methods for registering notification services
/// </summary>
public static class NotificationServiceExtensions
{
    /// <summary>
    /// Adds notification service to the service collection based on configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNotificationService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate configuration
        services.Configure<NotificationConfiguration>(
            configuration.GetSection(NotificationConfiguration.SectionName));

        // Add configuration as singleton with validation
        services.AddSingleton<NotificationConfiguration>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<NotificationConfiguration>>().Value;
            var errors = config.Validate();
            if (errors.Any())
            {
                throw new InvalidOperationException(
                    $"Invalid notification configuration: {string.Join(", ", errors)}");
            }
            return config;
        });

        // Register the notification service based on provider
        services.AddSingleton<INotificationService, NoOpNotificationService>();

        return services;
    }
}
