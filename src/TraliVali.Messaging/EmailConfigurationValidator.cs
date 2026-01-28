using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TraliVali.Messaging;

/// <summary>
/// Background service that validates email configuration at startup
/// </summary>
public class EmailConfigurationValidator : IHostedService
{
    private readonly AzureCommunicationEmailConfiguration _configuration;
    private readonly ILogger<EmailConfigurationValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailConfigurationValidator"/> class
    /// </summary>
    /// <param name="configuration">The email configuration to validate</param>
    /// <param name="logger">The logger instance</param>
    public EmailConfigurationValidator(
        AzureCommunicationEmailConfiguration configuration,
        ILogger<EmailConfigurationValidator> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating email configuration at startup...");

        var errors = _configuration.Validate();
        if (errors.Any())
        {
            var errorMessage = $"Invalid email configuration: {string.Join(", ", errors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation("Email configuration validated successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
