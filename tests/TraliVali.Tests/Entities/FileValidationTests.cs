using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for File entity validation
/// </summary>
public class FileValidationTests
{
    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenFileIsValid()
    {
        // Arrange
        var file = new TraliVali.Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439011",
            UploaderId = "507f1f77bcf86cd799439012",
            FileName = "test.txt",
            MimeType = "text/plain",
            Size = 1024,
            BlobPath = "/storage/files/test.txt"
        };

        // Act
        var errors = file.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenConversationIdIsEmpty()
    {
        // Arrange
        var file = new TraliVali.Domain.Entities.File
        {
            ConversationId = "",
            UploaderId = "507f1f77bcf86cd799439012",
            FileName = "test.txt",
            MimeType = "text/plain",
            Size = 1024,
            BlobPath = "/storage/files/test.txt"
        };

        // Act
        var errors = file.Validate();

        // Assert
        Assert.Contains("ConversationId is required", errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenUploaderIdIsEmpty()
    {
        // Arrange
        var file = new TraliVali.Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439011",
            UploaderId = "",
            FileName = "test.txt",
            MimeType = "text/plain",
            Size = 1024,
            BlobPath = "/storage/files/test.txt"
        };

        // Act
        var errors = file.Validate();

        // Assert
        Assert.Contains("UploaderId is required", errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenFileNameIsEmpty()
    {
        // Arrange
        var file = new TraliVali.Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439011",
            UploaderId = "507f1f77bcf86cd799439012",
            FileName = "",
            MimeType = "text/plain",
            Size = 1024,
            BlobPath = "/storage/files/test.txt"
        };

        // Act
        var errors = file.Validate();

        // Assert
        Assert.Contains("FileName is required", errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenMimeTypeIsEmpty()
    {
        // Arrange
        var file = new TraliVali.Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439011",
            UploaderId = "507f1f77bcf86cd799439012",
            FileName = "test.txt",
            MimeType = "",
            Size = 1024,
            BlobPath = "/storage/files/test.txt"
        };

        // Act
        var errors = file.Validate();

        // Assert
        Assert.Contains("MimeType is required", errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenSizeIsZero()
    {
        // Arrange
        var file = new TraliVali.Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439011",
            UploaderId = "507f1f77bcf86cd799439012",
            FileName = "test.txt",
            MimeType = "text/plain",
            Size = 0,
            BlobPath = "/storage/files/test.txt"
        };

        // Act
        var errors = file.Validate();

        // Assert
        Assert.Contains("Size must be greater than zero", errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenSizeIsNegative()
    {
        // Arrange
        var file = new TraliVali.Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439011",
            UploaderId = "507f1f77bcf86cd799439012",
            FileName = "test.txt",
            MimeType = "text/plain",
            Size = -100,
            BlobPath = "/storage/files/test.txt"
        };

        // Act
        var errors = file.Validate();

        // Assert
        Assert.Contains("Size must be greater than zero", errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenBlobPathIsEmpty()
    {
        // Arrange
        var file = new TraliVali.Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439011",
            UploaderId = "507f1f77bcf86cd799439012",
            FileName = "test.txt",
            MimeType = "text/plain",
            Size = 1024,
            BlobPath = ""
        };

        // Act
        var errors = file.Validate();

        // Assert
        Assert.Contains("BlobPath is required", errors);
    }
}
