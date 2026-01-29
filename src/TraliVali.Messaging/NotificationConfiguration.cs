namespace TraliVali.Messaging;

/// <summary>
/// Configuration for notification service
/// </summary>
public class NotificationConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Notifications";

    /// <summary>
    /// Gets or sets the notification provider type
    /// </summary>
    public string Provider { get; set; } = "None";

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Provider))
        {
            errors.Add("Provider is required");
        }
        else if (!IsValidProvider(Provider))
        {
            errors.Add($"Provider '{Provider}' is not supported. Valid values are: None");
        }

        return errors;
    }

    /// <summary>
    /// Checks if the provider value is valid
    /// </summary>
    private static bool IsValidProvider(string provider)
    {
        return provider.Equals("None", StringComparison.OrdinalIgnoreCase);
    }
}
