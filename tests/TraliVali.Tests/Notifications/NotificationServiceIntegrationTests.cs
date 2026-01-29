using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraliVali.Messaging;

namespace TraliVali.Tests.Notifications;

/// <summary>
/// Integration tests to verify notification service can be used in real scenarios
/// </summary>
public class NotificationServiceIntegrationTests
{
    [Fact]
    public async Task NotificationService_CanSendSingleNotification_Successfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Notifications:Provider", "None" }
            })
            .Build();

        services.AddNotificationService(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();

        // Act
        await notificationService.SendPushNotificationAsync("user123", "Test Title", "Test Body");

        // Assert - should complete without throwing
        Assert.NotNull(notificationService);
    }

    [Fact]
    public async Task NotificationService_CanSendBatchNotifications_Successfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Notifications:Provider", "None" }
            })
            .Build();

        services.AddNotificationService(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();

        // Act
        var userIds = new[] { "user1", "user2", "user3" };
        await notificationService.SendBatchNotificationsAsync(userIds, "Batch Title", "Batch Body");

        // Assert - should complete without throwing
        Assert.NotNull(notificationService);
    }

    [Fact]
    public void NotificationService_IsRegisteredAsSingleton_InRealScenario()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Notifications:Provider", "None" }
            })
            .Build();

        services.AddNotificationService(configuration);

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var instance1 = serviceProvider.GetService<INotificationService>();
        var instance2 = serviceProvider.GetService<INotificationService>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
    }
}
