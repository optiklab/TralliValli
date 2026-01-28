# Azure Communication Services Email Integration

This module provides email functionality for TraliVali using Azure Communication Services.

## Features

- **Magic Link Authentication**: Send passwordless authentication links to users
- **Invite Notifications**: Send platform invitations to new users
- **Password Reset**: Send secure password reset links
- **HTML Email Templates**: Professional, responsive email templates stored as embedded resources
- **Configuration Validation**: Automatic validation of email configuration on startup
- **Logging**: Comprehensive logging using Microsoft.Extensions.Logging

## Setup

### 1. Install Azure Communication Services

The required NuGet package is already included:
- `Azure.Communication.Email` (v1.0.1)

### 2. Configure appsettings.json

Add the following configuration section to your `appsettings.json`:

```json
{
  "AzureCommunicationEmail": {
    "ConnectionString": "endpoint=https://<your-resource>.communication.azure.com/;accesskey=<your-key>",
    "SenderAddress": "noreply@yourdomain.com",
    "SenderName": "TraliVali"
  }
}
```

### 3. Register the Service

In your `Program.cs` or `Startup.cs`, register the email service:

```csharp
using TraliVali.Messaging;

// Add email service
builder.Services.AddAzureCommunicationEmailService(builder.Configuration);
```

The service automatically:
- Validates the configuration on startup
- Throws an exception if configuration is invalid
- Registers as a singleton for optimal performance

## Usage

### Inject the Service

```csharp
public class YourService
{
    private readonly IEmailService _emailService;
    
    public YourService(IEmailService emailService)
    {
        _emailService = emailService;
    }
}
```

### Send a Magic Link Email

```csharp
await _emailService.SendMagicLinkEmailAsync(
    recipientEmail: "user@example.com",
    recipientName: "John Doe",
    magicLink: "https://yourapp.com/auth/magic?token=abc123",
    cancellationToken: cancellationToken
);
```

### Send an Invite Email

```csharp
await _emailService.SendInviteEmailAsync(
    recipientEmail: "newuser@example.com",
    recipientName: "Jane Smith",
    inviterName: "John Doe",
    inviteLink: "https://yourapp.com/invite?token=xyz789",
    cancellationToken: cancellationToken
);
```

### Send a Password Reset Email

```csharp
await _emailService.SendPasswordResetEmailAsync(
    recipientEmail: "user@example.com",
    recipientName: "John Doe",
    resetLink: "https://yourapp.com/reset?token=def456",
    cancellationToken: cancellationToken
);
```

## Email Templates

Email templates are stored as embedded resources in the `Templates` directory:
- `MagicLinkEmail.html` - Magic link authentication
- `InviteEmail.html` - User invitation
- `PasswordResetEmail.html` - Password reset

Templates use a simple placeholder syntax:
- `{{RecipientName}}` - Recipient's display name
- `{{MagicLink}}` - Magic authentication link
- `{{InviterName}}` - Name of person sending invite
- `{{InviteLink}}` - Invitation link
- `{{ResetLink}}` - Password reset link

## Configuration Validation

The service validates configuration on startup:
- `ConnectionString` must not be empty
- `SenderAddress` must be a valid email address
- `SenderName` is optional (defaults to "TraliVali")

If validation fails, an `InvalidOperationException` is thrown with detailed error messages.

## Error Handling

The service provides comprehensive error handling:
- Validates all input parameters
- Throws `ArgumentNullException` for null/empty required parameters
- Throws `InvalidOperationException` for Azure SDK errors
- Logs all operations and errors using `ILogger`

## Testing

Unit tests are provided in `tests/TraliVali.Tests/Email/`:
- `AzureCommunicationEmailConfigurationTests.cs` - Configuration validation tests
- `AzureCommunicationEmailServiceTests.cs` - Service behavior tests
- `EmailServiceExtensionsTests.cs` - Service registration tests

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~TraliVali.Tests.Email"
```

## Azure Communication Services Setup

1. Create an Azure Communication Services resource in Azure Portal
2. Configure Email Communication Service
3. Verify your sender domain
4. Get the connection string from the resource
5. Configure `appsettings.json` with your connection string and sender address

## Security Considerations

- Never commit connection strings to source control
- Use Azure Key Vault or environment variables for production
- Validate all email addresses before sending
- Use HTTPS for all links in emails
- Consider rate limiting for email sending

## Troubleshooting

### Configuration Errors
If you see "Invalid email configuration" errors:
1. Check that `ConnectionString` is properly formatted
2. Verify `SenderAddress` is a valid email address
3. Ensure the configuration section name is "AzureCommunicationEmail"

### Sending Errors
If emails fail to send:
1. Verify Azure Communication Services resource is active
2. Check that sender domain is verified
3. Ensure connection string has proper permissions
4. Review logs for detailed error messages

## Dependencies

- Azure.Communication.Email (1.0.1)
- Microsoft.Extensions.Configuration.Abstractions (8.0.0)
- Microsoft.Extensions.Configuration.Binder (8.0.0)
- Microsoft.Extensions.DependencyInjection.Abstractions (8.0.0)
- Microsoft.Extensions.Logging.Abstractions (8.0.0)
- Microsoft.Extensions.Options (8.0.0)
- Microsoft.Extensions.Options.ConfigurationExtensions (8.0.0)
