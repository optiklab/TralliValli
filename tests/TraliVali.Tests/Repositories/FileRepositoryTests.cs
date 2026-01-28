using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Tests.Infrastructure;

namespace TraliVali.Tests.Repositories;

/// <summary>
/// Tests for FileRepository
/// </summary>
public class FileRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly FileRepository _repository;

    public FileRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new FileRepository(_fixture.Context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddFile()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var file = new Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439010",
            UploaderId = "507f1f77bcf86cd799439011",
            FileName = "test.pdf",
            MimeType = "application/pdf",
            Size = 1024,
            BlobPath = "/storage/test.pdf",
            ContentType = "application/pdf",
            StoragePath = "/storage/test.pdf",
            UploadedBy = "507f1f77bcf86cd799439011",
            IsDeleted = false
        };

        // Act
        var result = await _repository.AddAsync(file);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("test.pdf", result.FileName);
        Assert.Equal(1024, result.Size);
    }

    [Fact]
    public async Task FindAsync_ShouldFindFilesByUploader()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var uploaderId = "507f1f77bcf86cd799439011";
        var file1 = new Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439010",
            UploaderId = uploaderId,
            FileName = "file1.pdf",
            MimeType = "application/pdf",
            Size = 1024,
            BlobPath = "/storage/file1.pdf",
            ContentType = "application/pdf",
            StoragePath = "/storage/file1.pdf",
            UploadedBy = uploaderId
        };
        var file2 = new Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439010",
            UploaderId = uploaderId,
            FileName = "file2.pdf",
            MimeType = "application/pdf",
            Size = 2048,
            BlobPath = "/storage/file2.pdf",
            ContentType = "application/pdf",
            StoragePath = "/storage/file2.pdf",
            UploadedBy = uploaderId
        };
        var file3 = new Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439010",
            UploaderId = "507f1f77bcf86cd799439012",
            FileName = "file3.pdf",
            MimeType = "application/pdf",
            Size = 512,
            BlobPath = "/storage/file3.pdf",
            ContentType = "application/pdf",
            StoragePath = "/storage/file3.pdf",
            UploadedBy = "507f1f77bcf86cd799439012"
        };

        await _repository.AddAsync(file1);
        await _repository.AddAsync(file2);
        await _repository.AddAsync(file3);

        // Act
        var result = await _repository.FindAsync(f => f.UploadedBy == uploaderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, f => Assert.Equal(uploaderId, f.UploadedBy));
    }

    [Fact]
    public async Task FindAsync_ShouldFindNonDeletedFiles()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var file1 = new Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439010",
            UploaderId = "507f1f77bcf86cd799439011",
            FileName = "active.pdf",
            MimeType = "application/pdf",
            Size = 1024,
            BlobPath = "/storage/active.pdf",
            ContentType = "application/pdf",
            StoragePath = "/storage/active.pdf",
            UploadedBy = "507f1f77bcf86cd799439011",
            IsDeleted = false
        };
        var file2 = new Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439010",
            UploaderId = "507f1f77bcf86cd799439011",
            FileName = "deleted.pdf",
            MimeType = "application/pdf",
            Size = 2048,
            BlobPath = "/storage/deleted.pdf",
            ContentType = "application/pdf",
            StoragePath = "/storage/deleted.pdf",
            UploadedBy = "507f1f77bcf86cd799439011",
            IsDeleted = true
        };

        await _repository.AddAsync(file1);
        await _repository.AddAsync(file2);

        // Act
        var result = await _repository.FindAsync(f => !f.IsDeleted);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.False(result.First().IsDeleted);
    }

    [Fact]
    public async Task UpdateAsync_ShouldMarkFileAsDeleted()
    {
        // Arrange
        await _fixture.CleanupAsync();
        var file = new Domain.Entities.File
        {
            ConversationId = "507f1f77bcf86cd799439010",
            UploaderId = "507f1f77bcf86cd799439011",
            FileName = "test.pdf",
            MimeType = "application/pdf",
            Size = 1024,
            BlobPath = "/storage/test.pdf",
            ContentType = "application/pdf",
            StoragePath = "/storage/test.pdf",
            UploadedBy = "507f1f77bcf86cd799439011",
            IsDeleted = false
        };
        var added = await _repository.AddAsync(file);
        added.IsDeleted = true;

        // Act
        var result = await _repository.UpdateAsync(added.Id, added);

        // Assert
        Assert.True(result);
        var updated = await _repository.GetByIdAsync(added.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsDeleted);
    }
}
