using System.Text;
using Testcontainers.Azurite;
using TraliVali.Infrastructure.Storage;

namespace TraliVali.Tests.Infrastructure.Storage;

/// <summary>
/// Integration tests for AzureBlobService using Azurite emulator
/// </summary>
public class AzureBlobServiceTests : IAsyncLifetime
{
    private AzuriteContainer? _azuriteContainer;
    private AzureBlobService? _blobService;
    private const string ContainerName = "test-archives";

    public async Task InitializeAsync()
    {
        // Start Azurite container
        _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest").Build();

        await _azuriteContainer.StartAsync();

        // Get connection string and create blob service
        var connectionString = _azuriteContainer.GetConnectionString();
        _blobService = new AzureBlobService(connectionString, ContainerName);
        
        // Ensure container exists
        await _blobService.EnsureContainerExistsAsync();
    }

    public async Task DisposeAsync()
    {
        if (_azuriteContainer != null)
        {
            await _azuriteContainer.StopAsync();
            await _azuriteContainer.DisposeAsync();
        }
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenConnectionStringIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AzureBlobService(null!, ContainerName));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenConnectionStringIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AzureBlobService(string.Empty, ContainerName));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenContainerNameIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AzureBlobService("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;", null!));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenContainerNameIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AzureBlobService("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;", string.Empty));
    }

    [Fact]
    public async Task UploadArchiveAsync_ShouldUploadBlob_WhenValidStreamAndPath()
    {
        // Arrange
        var path = "archives/2024/01/messages_conv123_2024-01-15.json";
        var content = "{\"test\": \"data\"}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        await _blobService!.UploadArchiveAsync(stream, path);

        // Assert - Verify upload by downloading
        var downloadedStream = await _blobService.DownloadArchiveAsync(path);
        using var reader = new StreamReader(downloadedStream);
        var downloadedContent = await reader.ReadToEndAsync();
        Assert.Equal(content, downloadedContent);
    }

    [Fact]
    public async Task UploadArchiveAsync_ShouldThrowArgumentNullException_WhenStreamIsNull()
    {
        // Arrange
        var path = "archives/2024/01/test.json";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _blobService!.UploadArchiveAsync(null!, path));
    }

    [Fact]
    public async Task UploadArchiveAsync_ShouldThrowArgumentException_WhenPathIsNull()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _blobService!.UploadArchiveAsync(stream, null!));
    }

    [Fact]
    public async Task UploadArchiveAsync_ShouldThrowArgumentException_WhenPathIsEmpty()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _blobService!.UploadArchiveAsync(stream, string.Empty));
    }

    [Fact]
    public async Task UploadArchiveAsync_ShouldOverwriteExistingBlob()
    {
        // Arrange
        var path = "archives/2024/01/messages_conv123_2024-01-15.json";
        var content1 = "{\"version\": 1}";
        var content2 = "{\"version\": 2}";

        // Act - Upload first version
        using (var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content1)))
        {
            await _blobService!.UploadArchiveAsync(stream1, path);
        }

        // Upload second version (overwrite)
        using (var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content2)))
        {
            await _blobService!.UploadArchiveAsync(stream2, path);
        }

        // Assert - Should have second version
        var downloadedStream = await _blobService!.DownloadArchiveAsync(path);
        using var reader = new StreamReader(downloadedStream);
        var downloadedContent = await reader.ReadToEndAsync();
        Assert.Equal(content2, downloadedContent);
    }

    [Fact]
    public async Task DownloadArchiveAsync_ShouldDownloadBlob_WhenBlobExists()
    {
        // Arrange
        var path = "archives/2024/01/messages_conv456_2024-01-15.json";
        var content = "{\"messageCount\": 42}";
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _blobService!.UploadArchiveAsync(uploadStream, path);

        // Act
        var downloadStream = await _blobService.DownloadArchiveAsync(path);

        // Assert
        using var reader = new StreamReader(downloadStream);
        var downloadedContent = await reader.ReadToEndAsync();
        Assert.Equal(content, downloadedContent);
    }

    [Fact]
    public async Task DownloadArchiveAsync_ShouldThrowFileNotFoundException_WhenBlobDoesNotExist()
    {
        // Arrange
        var path = "archives/2024/01/nonexistent.json";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _blobService!.DownloadArchiveAsync(path));
    }

    [Fact]
    public async Task DownloadArchiveAsync_ShouldThrowArgumentException_WhenPathIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _blobService!.DownloadArchiveAsync(null!));
    }

    [Fact]
    public async Task DownloadArchiveAsync_ShouldThrowArgumentException_WhenPathIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _blobService!.DownloadArchiveAsync(string.Empty));
    }

    [Fact]
    public async Task ListArchivesAsync_ShouldReturnEmptyList_WhenNoArchivesExist()
    {
        // Arrange
        var prefix = "archives/2025/12/";

        // Act
        var archives = await _blobService!.ListArchivesAsync(prefix);

        // Assert
        Assert.Empty(archives);
    }

    [Fact]
    public async Task ListArchivesAsync_ShouldReturnAllArchives_WhenPrefixMatches()
    {
        // Arrange
        var prefix = "archives/2024/01/";
        var paths = new[]
        {
            "archives/2024/01/messages_conv1_2024-01-15.json",
            "archives/2024/01/messages_conv2_2024-01-16.json",
            "archives/2024/01/messages_conv3_2024-01-17.json"
        };

        // Upload test archives
        foreach (var path in paths)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            await _blobService!.UploadArchiveAsync(stream, path);
        }

        // Act
        var archives = await _blobService!.ListArchivesAsync(prefix);

        // Assert
        var archiveList = archives.ToList();
        Assert.Equal(3, archiveList.Count);
        foreach (var path in paths)
        {
            Assert.Contains(path, archiveList);
        }
    }

    [Fact]
    public async Task ListArchivesAsync_ShouldNotReturnArchivesFromDifferentPrefix()
    {
        // Arrange
        var prefix1 = "archives/2024/01/";
        
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}")))
        {
            await _blobService!.UploadArchiveAsync(stream, "archives/2024/01/messages_conv1_2024-01-15.json");
        }
        
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}")))
        {
            await _blobService!.UploadArchiveAsync(stream, "archives/2024/02/messages_conv2_2024-02-15.json");
        }

        // Act
        var archives = await _blobService!.ListArchivesAsync(prefix1);

        // Assert
        var archiveList = archives.ToList();
        Assert.Single(archiveList);
        Assert.Contains("archives/2024/01/messages_conv1_2024-01-15.json", archiveList);
        Assert.DoesNotContain("archives/2024/02/messages_conv2_2024-02-15.json", archiveList);
    }

    [Fact]
    public async Task ListArchivesAsync_ShouldReturnAllArchives_WhenPrefixIsEmpty()
    {
        // Arrange
        var paths = new[]
        {
            "archives/2024/01/messages_conv1_2024-01-15.json",
            "archives/2024/02/messages_conv2_2024-02-16.json"
        };

        foreach (var path in paths)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            await _blobService!.UploadArchiveAsync(stream, path);
        }

        // Act
        var archives = await _blobService!.ListArchivesAsync(string.Empty);

        // Assert
        var archiveList = archives.ToList();
        Assert.True(archiveList.Count >= 2);
        foreach (var path in paths)
        {
            Assert.Contains(path, archiveList);
        }
    }

    [Fact]
    public async Task DeleteArchiveAsync_ShouldDeleteBlob_WhenBlobExists()
    {
        // Arrange
        var path = "archives/2024/01/messages_conv789_2024-01-15.json";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        await _blobService!.UploadArchiveAsync(stream, path);

        // Act
        var result = await _blobService.DeleteArchiveAsync(path);

        // Assert
        Assert.True(result);
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _blobService.DownloadArchiveAsync(path));
    }

    [Fact]
    public async Task DeleteArchiveAsync_ShouldReturnFalse_WhenBlobDoesNotExist()
    {
        // Arrange
        var path = "archives/2024/01/nonexistent_delete.json";

        // Act
        var result = await _blobService!.DeleteArchiveAsync(path);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteArchiveAsync_ShouldThrowArgumentException_WhenPathIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _blobService!.DeleteArchiveAsync(null!));
    }

    [Fact]
    public async Task DeleteArchiveAsync_ShouldThrowArgumentException_WhenPathIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _blobService!.DeleteArchiveAsync(string.Empty));
    }

    [Fact]
    public void GenerateArchivePath_ShouldGenerateCorrectPath()
    {
        // Arrange
        var conversationId = "conv123";
        var date = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var path = AzureBlobService.GenerateArchivePath(conversationId, date);

        // Assert
        Assert.Equal("archives/2024/01/messages_conv123_2024-01-15.json", path);
    }

    [Fact]
    public void GenerateArchivePath_ShouldPadMonthWithZero()
    {
        // Arrange
        var conversationId = "conv456";
        var date = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var path = AzureBlobService.GenerateArchivePath(conversationId, date);

        // Assert
        Assert.Equal("archives/2024/03/messages_conv456_2024-03-05.json", path);
    }

    [Fact]
    public void GenerateArchivePath_ShouldHandleDecemberCorrectly()
    {
        // Arrange
        var conversationId = "conv789";
        var date = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var path = AzureBlobService.GenerateArchivePath(conversationId, date);

        // Assert
        Assert.Equal("archives/2024/12/messages_conv789_2024-12-31.json", path);
    }

    [Fact]
    public void GenerateArchivePath_ShouldThrowArgumentException_WhenConversationIdIsNull()
    {
        // Arrange
        var date = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            AzureBlobService.GenerateArchivePath(null!, date));
    }

    [Fact]
    public void GenerateArchivePath_ShouldThrowArgumentException_WhenConversationIdIsEmpty()
    {
        // Arrange
        var date = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            AzureBlobService.GenerateArchivePath(string.Empty, date));
    }

    [Fact]
    public async Task ArchivePath_ShouldFollowSpecifiedStructure()
    {
        // Arrange - Test that the path structure matches: archives/{year}/{month}/messages_{conversationId}_{date}.json
        var conversationId = "conversation-12345";
        var date = new DateTime(2024, 6, 20, 14, 30, 0, DateTimeKind.Utc);
        var expectedPath = "archives/2024/06/messages_conversation-12345_2024-06-20.json";
        var content = "{\"messages\": []}";

        // Act
        var generatedPath = AzureBlobService.GenerateArchivePath(conversationId, date);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _blobService!.UploadArchiveAsync(stream, generatedPath);

        // Assert
        Assert.Equal(expectedPath, generatedPath);
        
        // Verify the archive exists at the expected path
        using var downloadedStream = await _blobService.DownloadArchiveAsync(expectedPath);
        Assert.NotNull(downloadedStream);
    }

    [Fact]
    public async Task MultipleArchives_ShouldBeOrganizedByYearAndMonth()
    {
        // Arrange
        var archives = new[]
        {
            ("conv1", new DateTime(2024, 1, 15)),
            ("conv2", new DateTime(2024, 1, 20)),
            ("conv3", new DateTime(2024, 2, 10)),
            ("conv4", new DateTime(2024, 3, 5))
        };

        // Act - Upload archives
        foreach (var (convId, date) in archives)
        {
            var path = AzureBlobService.GenerateArchivePath(convId, date);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            await _blobService!.UploadArchiveAsync(stream, path);
        }

        // Assert - List archives by month
        var januaryArchives = await _blobService!.ListArchivesAsync("archives/2024/01/");
        var februaryArchives = await _blobService.ListArchivesAsync("archives/2024/02/");
        var marchArchives = await _blobService.ListArchivesAsync("archives/2024/03/");

        Assert.Equal(2, januaryArchives.Count());
        Assert.Single(februaryArchives);
        Assert.Single(marchArchives);
    }

    [Fact]
    public void GenerateUploadUrl_ShouldGenerateValidUrl_WhenBlobPathIsValid()
    {
        // Arrange
        var blobPath = "archives/2024/01/test-upload.json";

        // Act
        var uploadUrl = _blobService!.GenerateUploadUrl(blobPath);

        // Assert
        Assert.NotNull(uploadUrl);
        Assert.NotEmpty(uploadUrl);
        Assert.Contains(blobPath, uploadUrl);
        Assert.Contains("sig=", uploadUrl); // SAS token signature
        Assert.Contains("se=", uploadUrl); // Expiry time
        Assert.Contains("sp=", uploadUrl); // Permissions
    }

    [Fact]
    public void GenerateUploadUrl_ShouldGenerateUrlWithCustomExpiry()
    {
        // Arrange
        var blobPath = "archives/2024/01/test-upload-custom.json";
        var expiresIn = TimeSpan.FromMinutes(30);

        // Act
        var uploadUrl = _blobService!.GenerateUploadUrl(blobPath, expiresIn);

        // Assert
        Assert.NotNull(uploadUrl);
        Assert.NotEmpty(uploadUrl);
        Assert.Contains(blobPath, uploadUrl);
        Assert.Contains("sig=", uploadUrl);
        Assert.Contains("se=", uploadUrl);
    }

    [Fact]
    public void GenerateUploadUrl_ShouldThrowArgumentException_WhenBlobPathIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _blobService!.GenerateUploadUrl(null!));
    }

    [Fact]
    public void GenerateUploadUrl_ShouldThrowArgumentException_WhenBlobPathIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _blobService!.GenerateUploadUrl(string.Empty));
    }

    [Fact]
    public void GenerateDownloadUrl_ShouldGenerateValidUrl_WhenBlobPathIsValid()
    {
        // Arrange
        var blobPath = "archives/2024/01/test-download.json";

        // Act
        var downloadUrl = _blobService!.GenerateDownloadUrl(blobPath);

        // Assert
        Assert.NotNull(downloadUrl);
        Assert.NotEmpty(downloadUrl);
        Assert.Contains(blobPath, downloadUrl);
        Assert.Contains("sig=", downloadUrl); // SAS token signature
        Assert.Contains("se=", downloadUrl); // Expiry time
        Assert.Contains("sp=", downloadUrl); // Permissions
    }

    [Fact]
    public void GenerateDownloadUrl_ShouldGenerateUrlWithCustomExpiry()
    {
        // Arrange
        var blobPath = "archives/2024/01/test-download-custom.json";
        var expiresIn = TimeSpan.FromHours(2);

        // Act
        var downloadUrl = _blobService!.GenerateDownloadUrl(blobPath, expiresIn);

        // Assert
        Assert.NotNull(downloadUrl);
        Assert.NotEmpty(downloadUrl);
        Assert.Contains(blobPath, downloadUrl);
        Assert.Contains("sig=", downloadUrl);
        Assert.Contains("se=", downloadUrl);
    }

    [Fact]
    public void GenerateDownloadUrl_ShouldThrowArgumentException_WhenBlobPathIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _blobService!.GenerateDownloadUrl(null!));
    }

    [Fact]
    public void GenerateDownloadUrl_ShouldThrowArgumentException_WhenBlobPathIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _blobService!.GenerateDownloadUrl(string.Empty));
    }

    [Fact]
    public async Task GenerateUploadUrl_ShouldAllowUploadUsingPresignedUrl()
    {
        // Arrange
        var blobPath = "archives/2024/01/presigned-upload-test.json";
        var content = "{\"test\": \"presigned upload\"}";
        var uploadUrl = _blobService!.GenerateUploadUrl(blobPath);

        // Act - Upload using the presigned URL
        using var httpClient = new HttpClient();
        using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var httpContent = new StreamContent(contentStream);
        httpContent.Headers.Add("x-ms-blob-type", "BlockBlob");
        
        var response = await httpClient.PutAsync(uploadUrl, httpContent);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Upload failed with status {response.StatusCode}");
        
        // Verify the blob was uploaded by downloading it
        var downloadedStream = await _blobService.DownloadArchiveAsync(blobPath);
        using var reader = new StreamReader(downloadedStream);
        var downloadedContent = await reader.ReadToEndAsync();
        Assert.Equal(content, downloadedContent);
    }

    [Fact]
    public async Task GenerateDownloadUrl_ShouldAllowDownloadUsingPresignedUrl()
    {
        // Arrange
        var blobPath = "archives/2024/01/presigned-download-test.json";
        var content = "{\"test\": \"presigned download\"}";
        
        // Upload the blob first
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _blobService!.UploadArchiveAsync(uploadStream, blobPath);
        
        // Generate download URL
        var downloadUrl = _blobService.GenerateDownloadUrl(blobPath);

        // Act - Download using the presigned URL
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(downloadUrl);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Download failed with status {response.StatusCode}");
        
        var downloadedContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(content, downloadedContent);
    }

    [Fact]
    public void GenerateUploadUrl_And_GenerateDownloadUrl_ShouldGenerateDifferentUrls()
    {
        // Arrange
        var blobPath = "archives/2024/01/test-different-urls.json";

        // Act
        var uploadUrl = _blobService!.GenerateUploadUrl(blobPath);
        var downloadUrl = _blobService!.GenerateDownloadUrl(blobPath);

        // Assert
        Assert.NotEqual(uploadUrl, downloadUrl);
        // Both should contain the blob path but have different permissions
        Assert.Contains(blobPath, uploadUrl);
        Assert.Contains(blobPath, downloadUrl);
    }
}
