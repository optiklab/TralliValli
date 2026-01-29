# TraliVali Messaging Module

This module provides messaging functionality for TraliVali, including email and push notifications.

## Features

- **Email Integration**: Azure Communication Services email functionality
  - Magic Link Authentication
  - Invite Notifications
  - Password Reset
  - Welcome Emails
- **Push Notifications**: Extensible notification service with stub implementation
- **HTML Email Templates**: Professional, responsive email templates
- **Configuration Validation**: Automatic validation on startup
- **Logging**: Comprehensive logging using Microsoft.Extensions.Logging

---

## Push Notifications

### Overview

The notification service provides a pluggable architecture for sending push notifications to users. Currently, a no-operation (NoOp) stub implementation is included for development and testing purposes.

### Features

- **Single User Notifications**: Send push notifications to individual users
- **Batch Notifications**: Send the same notification to multiple users simultaneously
- **Stub Implementation**: NoOpNotificationService logs notifications without sending them
- **Future-Ready**: Interface designed for easy integration with real notification providers

### Setup

#### 1. Configure appsettings.json

Add the following configuration section to your `appsettings.json`:

```json
{
  "Notifications": {
    "Provider": "None"
  }
}
```

**Configuration Options:**
- `Provider`: The notification provider to use
  - `"None"`: Uses NoOpNotificationService (logs only, no actual notifications sent)
  - Future providers will be added here (e.g., "Firebase", "OneSignal", "AzureNotificationHub")

#### 2. Register the Service

The service is automatically registered in `Program.cs`:

```csharp
using TraliVali.Messaging;

// Add notification service
builder.Services.AddNotificationService(builder.Configuration);
```

The service is registered as a singleton for optimal performance.

### Usage

#### Inject the Service

```csharp
public class YourService
{
    private readonly INotificationService _notificationService;
    
    public YourService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
}
```

#### Send a Push Notification to a Single User

```csharp
await _notificationService.SendPushNotificationAsync(
    userId: "user123",
    title: "New Message",
    body: "You have a new message from John",
    cancellationToken: cancellationToken
);
```

#### Send Batch Notifications to Multiple Users

```csharp
var userIds = new[] { "user1", "user2", "user3" };
await _notificationService.SendBatchNotificationsAsync(
    userIds: userIds,
    title: "System Maintenance",
    body: "System will undergo maintenance at 2 AM",
    cancellationToken: cancellationToken
);
```

### Current Implementation: NoOpNotificationService

The `NoOpNotificationService` is a stub implementation that:
- ✅ Validates all input parameters
- ✅ Logs notification details at Information level
- ✅ Returns successfully without sending actual notifications
- ✅ Useful for development, testing, and when notifications are disabled

**Example Log Output:**
```
[12:34:56 INF] NoOpNotificationService initialized - notifications will be logged but not sent
[12:35:01 INF] Would send push notification to user user123: Title='New Message', Body='You have a new message from John'
[12:35:10 INF] Would send batch notification to 3 users: Title='System Maintenance', Body='System will undergo maintenance at 2 AM', UserIds=[user1, user2, user3]
```

### Future Implementation

To implement a real notification provider:

1. **Create a new provider implementation:**
   ```csharp
   public class FirebaseNotificationService : INotificationService
   {
       // Implement SendPushNotificationAsync
       // Implement SendBatchNotificationsAsync
   }
   ```

2. **Add configuration support:**
   - Update `NotificationConfiguration` to support the new provider
   - Add provider-specific configuration properties
   - Update validation logic

3. **Update service registration:**
   - Modify `NotificationServiceExtensions.AddNotificationService()`
   - Add conditional registration based on `Provider` setting

4. **Update documentation:**
   - Add setup instructions for the new provider
   - Document configuration options
   - Provide usage examples

### Testing

Unit tests are provided in `tests/TraliVali.Tests/Notifications/`:
- `NoOpNotificationServiceTests.cs` - Service behavior tests
- `NotificationConfigurationTests.cs` - Configuration validation tests
- `NotificationServiceExtensionsTests.cs` - Service registration tests

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~TraliVali.Tests.Notifications"
```

### Error Handling

The service provides comprehensive error handling:
- Validates all input parameters
- Throws `ArgumentNullException` for null/empty required parameters
- Logs all operations and errors using `ILogger`
- Future implementations should throw `InvalidOperationException` for provider-specific errors

### Interface Reference

```csharp
public interface INotificationService
{
    Task SendPushNotificationAsync(
        string userId, 
        string title, 
        string body, 
        CancellationToken cancellationToken = default);

    Task SendBatchNotificationsAsync(
        string[] userIds, 
        string title, 
        string body, 
        CancellationToken cancellationToken = default);
}
```

**Parameters:**
- `userId` / `userIds`: User identifier(s) - must not be null or empty
- `title`: Notification title - must not be null or empty  
- `body`: Notification content - must not be null or empty
- `cancellationToken`: Optional cancellation token

---

## Email Integration (Azure Communication Services)

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
