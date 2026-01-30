using System.Text.Json;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Messaging;
using TraliVali.Infrastructure.Storage;
using TraliVali.Workers;
using TraliVali.Workers.Models;

namespace TraliVali.Tests.Workers;

/// <summary>
/// Tests for FileProcessorWorker
/// Note: Full integration tests for file processing require actual blob storage and MongoDB.
/// These tests focus on constructor validation and basic structure verification.
/// </summary>
public class FileProcessorWorkerTests : IDisposable
{
    private readonly Mock<IMessageConsumer> _mockConsumer;
    private readonly Mock<IMessagePublisher> _mockPublisher;
    private readonly Mock<ILogger<FileProcessorWorker>> _mockLogger;
    private readonly FileProcessorWorkerConfiguration _configuration;
    private readonly Mock<IMongoCollection<Domain.Entities.File>> _mockFileCollection;
    private readonly Mock<IAzureBlobService> _mockBlobService;

    public FileProcessorWorkerTests()
    {
        _mockConsumer = new Mock<IMessageConsumer>();
        _mockPublisher = new Mock<IMessagePublisher>();
        _mockLogger = new Mock<ILogger<FileProcessorWorker>>();
        _mockBlobService = new Mock<IAzureBlobService>();
        
        // Create mock collection
        _mockFileCollection = new Mock<IMongoCollection<Domain.Entities.File>>();

        _configuration = new FileProcessorWorkerConfiguration
        {
            DeadLetterQueueName = "files.process.deadletter",
            MaxRetryAttempts = 3,
            ThumbnailMaxDimension = 300
        };
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConsumerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileProcessorWorker(
            null!,
            _mockPublisher.Object,
            _mockFileCollection.Object,
            _mockBlobService.Object,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPublisherIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileProcessorWorker(
            _mockConsumer.Object,
            null!,
            _mockFileCollection.Object,
            _mockBlobService.Object,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenFilesCollectionIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            null!,
            _mockBlobService.Object,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenBlobServiceIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            _mockFileCollection.Object,
            null!,
            _configuration,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            _mockFileCollection.Object,
            _mockBlobService.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            _mockFileCollection.Object,
            _mockBlobService.Object,
            _configuration,
            null!));
    }

    [Fact]
    public void Constructor_ShouldCreateWorkerSuccessfully_WhenAllParametersAreValid()
    {
        // Act
        var worker = new FileProcessorWorker(
            _mockConsumer.Object,
            _mockPublisher.Object,
            _mockFileCollection.Object,
            _mockBlobService.Object,
            _configuration,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void FileQueuePayload_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var payload = new FileQueuePayload
        {
            FileId = "507f1f77bcf86cd799439011",
            BlobPath = "files/conversation123/file456/image.jpg",
            MimeType = "image/jpeg",
            FileName = "image.jpg"
        };

        // Act
        var json = JsonSerializer.Serialize(payload);
        var deserialized = JsonSerializer.Deserialize<FileQueuePayload>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(payload.FileId, deserialized.FileId);
        Assert.Equal(payload.BlobPath, deserialized.BlobPath);
        Assert.Equal(payload.MimeType, deserialized.MimeType);
        Assert.Equal(payload.FileName, deserialized.FileName);
    }

    [Fact]
    public void FileProcessorWorkerConfiguration_ShouldHaveDefaultValues()
    {
        // Act
        var config = new FileProcessorWorkerConfiguration();

        // Assert
        Assert.Equal("files.process.deadletter", config.DeadLetterQueueName);
        Assert.Equal(3, config.MaxRetryAttempts);
        Assert.Equal(300, config.ThumbnailMaxDimension);
    }

    [Fact]
    public void FileProcessorWorkerConfiguration_ShouldAllowCustomValues()
    {
        // Arrange & Act
        var config = new FileProcessorWorkerConfiguration
        {
            DeadLetterQueueName = "custom.deadletter",
            MaxRetryAttempts = 5,
            ThumbnailMaxDimension = 500
        };

        // Assert
        Assert.Equal("custom.deadletter", config.DeadLetterQueueName);
        Assert.Equal(5, config.MaxRetryAttempts);
        Assert.Equal(500, config.ThumbnailMaxDimension);
    }

    [Fact]
    public void FileEntity_ShouldHaveMetadataFields()
    {
        // Arrange & Act
        var file = new Domain.Entities.File
        {
            Id = "507f1f77bcf86cd799439011",
            ConversationId = "507f1f77bcf86cd799439012",
            UploaderId = "507f1f77bcf86cd799439013",
            FileName = "test.jpg",
            MimeType = "image/jpeg",
            Size = 1024,
            BlobPath = "files/test.jpg",
            ThumbnailPath = "thumbnails/test_thumb.jpg",
            Width = 1920,
            Height = 1080,
            Duration = 120.5,
            ExifData = "{\"Make\":\"Canon\",\"Model\":\"EOS 5D\"}"
        };

        // Assert
        Assert.NotNull(file);
        Assert.Equal("thumbnails/test_thumb.jpg", file.ThumbnailPath);
        Assert.Equal(1920, file.Width);
        Assert.Equal(1080, file.Height);
        Assert.Equal(120.5, file.Duration);
        Assert.Equal("{\"Make\":\"Canon\",\"Model\":\"EOS 5D\"}", file.ExifData);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
