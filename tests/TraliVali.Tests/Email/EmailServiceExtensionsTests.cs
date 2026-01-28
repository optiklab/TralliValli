using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraliVali.Messaging;

namespace TraliVali.Tests.Email;

/// <summary>
/// Tests for EmailServiceExtensions
/// </summary>
public class EmailServiceExtensionsTests
{
    [Fact]
    public void AddAzureCommunicationEmailService_WithValidConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = CreateConfiguration(
            "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==",
            "noreply@example.com",
            "Test Sender");

        // Act
        services.AddAzureCommunicationEmailService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var emailService = serviceProvider.GetService<IEmailService>();
        Assert.NotNull(emailService);
        Assert.IsType<AzureCommunicationEmailService>(emailService);

        var config = serviceProvider.GetService<AzureCommunicationEmailConfiguration>();
        Assert.NotNull(config);
        Assert.Equal("endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==", config.ConnectionString);
        Assert.Equal("noreply@example.com", config.SenderAddress);
        Assert.Equal("Test Sender", config.SenderName);
    }

    [Fact]
    public void AddAzureCommunicationEmailService_WithInvalidConfiguration_ShouldThrowOnServiceResolution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = CreateConfiguration("", "", "Test Sender"); // Invalid config

        // Act
        services.AddAzureCommunicationEmailService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Throws<InvalidOperationException>(() => 
            serviceProvider.GetRequiredService<AzureCommunicationEmailConfiguration>());
    }

    [Fact]
    public void AddAzureCommunicationEmailService_WithMissingConnectionString_ShouldThrowOnServiceResolution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = CreateConfiguration("", "noreply@example.com", "Test Sender");

        // Act
        services.AddAzureCommunicationEmailService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            serviceProvider.GetRequiredService<AzureCommunicationEmailConfiguration>());
        Assert.Contains("ConnectionString is required", exception.Message);
    }

    [Fact]
    public void AddAzureCommunicationEmailService_WithMissingSenderAddress_ShouldThrowOnServiceResolution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = CreateConfiguration("endpoint=https://test.communication.azure.com/;accesskey=testkey", "", "Test Sender");

        // Act
        services.AddAzureCommunicationEmailService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            serviceProvider.GetRequiredService<AzureCommunicationEmailConfiguration>());
        Assert.Contains("SenderAddress is required", exception.Message);
    }

    [Fact]
    public void AddAzureCommunicationEmailService_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging support
        var configuration = CreateConfiguration(
            "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==",
            "noreply@example.com",
            "Test Sender");

        // Act
        services.AddAzureCommunicationEmailService(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service1 = serviceProvider.GetService<IEmailService>();
        var service2 = serviceProvider.GetService<IEmailService>();
        Assert.Same(service1, service2); // Should be the same instance (singleton)
    }

    private static IConfiguration CreateConfiguration(string connectionString, string senderAddress, string senderName)
    {
        var configData = new Dictionary<string, string?>
        {
            { "AzureCommunicationEmail:ConnectionString", connectionString },
            { "AzureCommunicationEmail:SenderAddress", senderAddress },
            { "AzureCommunicationEmail:SenderName", senderName }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}
