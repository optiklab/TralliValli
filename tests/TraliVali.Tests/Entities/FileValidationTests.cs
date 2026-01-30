using TraliVali.Domain.Entities;

namespace TraliVali.Tests.Entities;

/// <summary>
/// Tests for File entity validation following Given-When-Then pattern
/// </summary>
public class FileValidationTests
{
    [Fact]
    public void GivenValidFile_WhenValidating_ThenReturnsNoErrors()
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
    public void GivenEmptyConversationId_WhenValidating_ThenReturnsConversationIdRequiredError()
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
    public void GivenEmptyUploaderId_WhenValidating_ThenReturnsUploaderIdRequiredError()
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
    public void GivenEmptyFileName_WhenValidating_ThenReturnsFileNameRequiredError()
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
    public void GivenEmptyMimeType_WhenValidating_ThenReturnsMimeTypeRequiredError()
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
    public void GivenZeroSize_WhenValidating_ThenReturnsSizeGreaterThanZeroError()
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
    public void GivenNegativeSize_WhenValidating_ThenReturnsSizeGreaterThanZeroError()
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
    public void GivenEmptyBlobPath_WhenValidating_ThenReturnsBlobPathRequiredError()
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
