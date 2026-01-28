namespace TraliVali.Messaging;

/// <summary>
/// Configuration for Azure Communication Services Email
/// </summary>
public class AzureCommunicationEmailConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "AzureCommunicationEmail";

    /// <summary>
    /// Gets or sets the Azure Communication Services connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender email address
    /// </summary>
    public string SenderAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender display name (optional)
    /// </summary>
    public string SenderName { get; set; } = "TraliVali";

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>A list of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            errors.Add("ConnectionString is required");

        if (string.IsNullOrWhiteSpace(SenderAddress))
            errors.Add("SenderAddress is required");
        else if (!IsValidEmail(SenderAddress))
            errors.Add("SenderAddress format is invalid");

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
