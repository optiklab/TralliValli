using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TraliVali.Messaging;

/// <summary>
/// Extension methods for registering email services
/// </summary>
public static class EmailServiceExtensions
{
    /// <summary>
    /// Adds Azure Communication Email Service to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAzureCommunicationEmailService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate configuration
        services.Configure<AzureCommunicationEmailConfiguration>(
            configuration.GetSection(AzureCommunicationEmailConfiguration.SectionName));

        // Add validation on startup
        services.AddSingleton<AzureCommunicationEmailConfiguration>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AzureCommunicationEmailConfiguration>>().Value;
            var errors = config.Validate();
            if (errors.Any())
            {
                throw new InvalidOperationException(
                    $"Invalid Azure Communication Email configuration: {string.Join(", ", errors)}");
            }
            return config;
        });

        // Register the email service
        services.AddSingleton<IEmailService, AzureCommunicationEmailService>();

        return services;
    }
}
