using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraliVali.Messaging;

namespace TraliVali.Tests.Notifications;

/// <summary>
/// Tests for NotificationServiceExtensions
/// </summary>
public class NotificationServiceExtensionsTests
{
    [Fact]
    public void AddNotificationService_WithValidConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Notifications:Provider", "None" }
            })
            .Build();

        // Act
        services.AddNotificationService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var notificationService = serviceProvider.GetService<INotificationService>();
        Assert.NotNull(notificationService);
        Assert.IsType<NoOpNotificationService>(notificationService);
    }

    [Fact]
    public void AddNotificationService_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Notifications:Provider", "None" }
            })
            .Build();

        // Act
        services.AddNotificationService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service1 = serviceProvider.GetService<INotificationService>();
        var service2 = serviceProvider.GetService<INotificationService>();
        Assert.Same(service1, service2);
    }

    [Fact]
    public void AddNotificationService_WithMissingConfiguration_ShouldRegisterServicesWithDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddNotificationService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Should use default "None" provider
        var notificationService = serviceProvider.GetService<INotificationService>();
        Assert.NotNull(notificationService);
        Assert.IsType<NoOpNotificationService>(notificationService);
    }

    [Fact]
    public void AddNotificationService_WithInvalidProvider_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Notifications:Provider", "InvalidProvider" }
            })
            .Build();

        // Act
        services.AddNotificationService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Throws<InvalidOperationException>(() => 
            serviceProvider.GetRequiredService<NotificationConfiguration>());
    }

    [Fact]
    public void AddNotificationService_ShouldRegisterConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Notifications:Provider", "None" }
            })
            .Build();

        // Act
        services.AddNotificationService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var config = serviceProvider.GetService<NotificationConfiguration>();
        Assert.NotNull(config);
        Assert.Equal("None", config.Provider);
    }
}
