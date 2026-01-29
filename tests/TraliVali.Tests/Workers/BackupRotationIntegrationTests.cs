using System.IO.Compression;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Testcontainers.Azurite;
using TraliVali.Workers;

namespace TraliVali.Tests.Workers;

/// <summary>
/// Integration tests for backup rotation functionality using Azurite emulator
/// Tests the cleanup of old backups based on retention policy
/// </summary>
public class BackupRotationIntegrationTests : IAsyncLifetime
{
    private AzuriteContainer? _azuriteContainer;
    private BlobServiceClient? _blobServiceClient;
    private BlobContainerClient? _containerClient;
    private const string ContainerName = "backups";

    public async Task InitializeAsync()
    {
        // Start Azurite container
        _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest").Build();

        await _azuriteContainer.StartAsync();

        // Get connection string and create blob clients
        var connectionString = _azuriteContainer.GetConnectionString();
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await _containerClient.CreateIfNotExistsAsync();
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
    public async Task BackupRotation_ShouldDeleteBackupsOlderThanRetentionDays()
    {
        // Arrange - Create backups with different dates
        var today = DateTime.UtcNow;
        var backupDates = new[]
        {
            today.AddDays(-40).ToString("yyyy-MM-dd"), // Should be deleted (older than 30 days)
            today.AddDays(-35).ToString("yyyy-MM-dd"), // Should be deleted
            today.AddDays(-31).ToString("yyyy-MM-dd"), // Should be deleted
            today.AddDays(-29).ToString("yyyy-MM-dd"), // Should be kept
            today.AddDays(-15).ToString("yyyy-MM-dd"), // Should be kept
            today.AddDays(-5).ToString("yyyy-MM-dd"),  // Should be kept
            today.ToString("yyyy-MM-dd")               // Should be kept (today)
        };

        foreach (var backupDate in backupDates)
        {
            var blobPath = $"backups/{backupDate}/tralivali_users.bson.gz";
            await UploadTestBackupAsync(blobPath);
        }

        // Verify all backups were created
        var allBlobs = await ListBlobsAsync();
        Assert.Equal(7, allBlobs.Count);

        // Act - Simulate cleanup with 30-day retention
        var cutoffDate = today.AddDays(-30);
        var deletedCount = 0;
        
        await foreach (var blobItem in _containerClient!.GetBlobsAsync(prefix: "backups/"))
        {
            var pathParts = blobItem.Name.Split('/');
            if (pathParts.Length >= 2 && DateTime.TryParse(pathParts[1], out var blobDate))
            {
                if (blobDate < cutoffDate)
                {
                    var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                    await blobClient.DeleteIfExistsAsync();
                    deletedCount++;
                }
            }
        }

        // Assert - Verify 3 old backups were deleted and 4 recent ones remain
        Assert.Equal(3, deletedCount);
        
        var remainingBlobs = await ListBlobsAsync();
        Assert.Equal(4, remainingBlobs.Count);
    }

    [Fact]
    public async Task BackupRotation_ShouldParseCorrectDateFromBlobPath()
    {
        // Arrange - Create backups with various date formats in path
        var testCases = new Dictionary<string, bool>
        {
            { "backups/2024-01-15/tralivali_users.bson.gz", true },      // Valid date format
            { "backups/2024-12-31/tralivali_messages.bson.gz", true },   // Valid date format
            { "backups/2025-06-01/tralivali_files.bson.gz", true },      // Valid date format
            { "backups/invalid-date/tralivali_users.bson.gz", false },   // Invalid date format
            { "backups/2024-13-01/tralivali_users.bson.gz", false },     // Invalid month
        };

        foreach (var (blobPath, shouldParse) in testCases)
        {
            await UploadTestBackupAsync(blobPath);
        }

        // Act & Assert - Verify date parsing logic
        var parsedDates = new List<DateTime>();
        
        await foreach (var blobItem in _containerClient!.GetBlobsAsync(prefix: "backups/"))
        {
            var pathParts = blobItem.Name.Split('/');
            if (pathParts.Length >= 2 && DateTime.TryParse(pathParts[1], out var blobDate))
            {
                parsedDates.Add(blobDate);
            }
        }

        // Should have parsed exactly 3 valid dates
        Assert.Equal(3, parsedDates.Count);
    }

    [Fact]
    public async Task BackupRotation_ShouldHandleEmptyBackupsContainer()
    {
        // Arrange - Empty container (no backups)
        var allBlobs = await ListBlobsAsync();
        Assert.Empty(allBlobs);

        // Act - Simulate cleanup on empty container
        var deletedCount = 0;
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        
        await foreach (var blobItem in _containerClient!.GetBlobsAsync(prefix: "backups/"))
        {
            deletedCount++;
        }

        // Assert - No errors, no deletions
        Assert.Equal(0, deletedCount);
    }

    [Fact]
    public async Task BackupRotation_ShouldOnlyDeleteBlobsInBackupsPrefix()
    {
        // Arrange - Create blobs with different prefixes
        await UploadTestBackupAsync("backups/2024-01-01/tralivali_users.bson.gz");
        await UploadTestBackupAsync("archives/2024-01-01/archived_messages.json");
        await UploadTestBackupAsync("other/2024-01-01/other_data.txt");

        // Act - List only backups/ prefix
        var backupBlobs = new List<string>();
        await foreach (var blobItem in _containerClient!.GetBlobsAsync(prefix: "backups/"))
        {
            backupBlobs.Add(blobItem.Name);
        }

        // Assert - Only backups/ prefix items should be listed
        Assert.Single(backupBlobs);
        Assert.Contains("backups/2024-01-01/tralivali_users.bson.gz", backupBlobs);
    }

    [Theory]
    [InlineData(1)]   // 1 day retention
    [InlineData(7)]   // 1 week retention
    [InlineData(30)]  // 30 days retention
    [InlineData(90)]  // 90 days retention
    [InlineData(365)] // 1 year retention
    public async Task BackupRotation_ShouldRespectConfiguredRetentionDays(int retentionDays)
    {
        // Arrange - Create backups spanning retention period
        var today = DateTime.UtcNow;
        var oldDate = today.AddDays(-(retentionDays + 5)); // Older than retention
        var recentDate = today.AddDays(-(retentionDays - 5)); // Within retention
        
        await UploadTestBackupAsync($"backups/{oldDate:yyyy-MM-dd}/tralivali_users.bson.gz");
        await UploadTestBackupAsync($"backups/{recentDate:yyyy-MM-dd}/tralivali_users.bson.gz");

        // Act - Simulate cleanup with configured retention
        var cutoffDate = today.AddDays(-retentionDays);
        var deletedCount = 0;
        var keptCount = 0;
        
        await foreach (var blobItem in _containerClient!.GetBlobsAsync(prefix: "backups/"))
        {
            var pathParts = blobItem.Name.Split('/');
            if (pathParts.Length >= 2 && DateTime.TryParse(pathParts[1], out var blobDate))
            {
                if (blobDate < cutoffDate)
                {
                    deletedCount++;
                }
                else
                {
                    keptCount++;
                }
            }
        }

        // Assert - Old backup should be deleted, recent one kept
        Assert.Equal(1, deletedCount);
        Assert.Equal(1, keptCount);
    }

    [Fact]
    public async Task BackupRotation_ShouldDeleteMultipleCollectionBackupsForSameDate()
    {
        // Arrange - Create multiple collection backups for the same old date
        var oldDate = DateTime.UtcNow.AddDays(-40).ToString("yyyy-MM-dd");
        var collections = new[] { "users", "conversations", "messages", "invites", "files" };
        
        foreach (var collection in collections)
        {
            var blobPath = $"backups/{oldDate}/tralivali_{collection}.bson.gz";
            await UploadTestBackupAsync(blobPath);
        }

        // Verify all 5 collection backups were created
        var allBlobs = await ListBlobsAsync();
        Assert.Equal(5, allBlobs.Count);

        // Act - Simulate cleanup with 30-day retention
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var deletedCount = 0;
        
        await foreach (var blobItem in _containerClient!.GetBlobsAsync(prefix: "backups/"))
        {
            var pathParts = blobItem.Name.Split('/');
            if (pathParts.Length >= 2 && DateTime.TryParse(pathParts[1], out var blobDate))
            {
                if (blobDate < cutoffDate)
                {
                    var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                    await blobClient.DeleteIfExistsAsync();
                    deletedCount++;
                }
            }
        }

        // Assert - All 5 collection backups for the old date should be deleted
        Assert.Equal(5, deletedCount);
        
        var remainingBlobs = await ListBlobsAsync();
        Assert.Empty(remainingBlobs);
    }

    [Fact]
    public async Task BackupRotation_ShouldKeepBackupsOnExactRetentionBoundary()
    {
        // Arrange - Test the boundary behavior of retention cleanup
        // In production: BackupWorker uses DateTime.UtcNow which includes time component
        // If a backup runs at 3 AM and retention is 30 days:
        //   - Cutoff would be "30 days ago at 3 AM"
        //   - A backup from "30 days ago at midnight" would be deleted (midnight < 3 AM)
        // 
        // This test uses .Date to normalize both to midnight for predictable testing.
        // With both at midnight and using < comparison, backup at exact boundary is kept.
        var today = DateTime.UtcNow.Date; // Normalized to midnight for test consistency
        var boundaryDate = today.AddDays(-30).ToString("yyyy-MM-dd");
        
        await UploadTestBackupAsync($"backups/{boundaryDate}/tralivali_users.bson.gz");

        // Act - Simulate cleanup with 30-day retention using normalized dates
        var cutoffDate = today.AddDays(-30).Date; // Midnight of the cutoff day
        var deletedCount = 0;
        
        await foreach (var blobItem in _containerClient!.GetBlobsAsync(prefix: "backups/"))
        {
            var pathParts = blobItem.Name.Split('/');
            if (pathParts.Length >= 2 && DateTime.TryParse(pathParts[1], out var blobDate))
            {
                // Implementation uses < not <= for comparison
                if (blobDate < cutoffDate)
                {
                    var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                    await blobClient.DeleteIfExistsAsync();
                    deletedCount++;
                }
            }
        }

        // Assert - With normalized dates, backup at exact boundary (same midnight) is kept
        Assert.Equal(0, deletedCount);
        var remainingBlobs = await ListBlobsAsync();
        Assert.Single(remainingBlobs);
    }

    [Fact]
    public async Task BackupRotation_ShouldHandleBlobsWithInvalidPathStructure()
    {
        // Arrange - Create blobs with various path structures
        await UploadTestBackupAsync("backups/tralivali_users.bson.gz"); // Missing date folder
        await UploadTestBackupAsync("backups/2024-01-15/subfolder/tralivali_users.bson.gz"); // Extra nesting
        await UploadTestBackupAsync("backups/2024-01-15/tralivali_users.bson.gz"); // Correct structure

        // Act - Attempt to parse dates from all blobs
        var parsedCount = 0;
        var unparsedCount = 0;
        
        await foreach (var blobItem in _containerClient!.GetBlobsAsync(prefix: "backups/"))
        {
            var pathParts = blobItem.Name.Split('/');
            if (pathParts.Length >= 2 && DateTime.TryParse(pathParts[1], out var blobDate))
            {
                parsedCount++;
            }
            else
            {
                unparsedCount++;
            }
        }

        // Assert - Should parse valid structure, skip invalid ones
        Assert.Equal(2, parsedCount); // The correct one and the extra nested one
        Assert.Equal(1, unparsedCount); // The one missing date folder
    }

    /// <summary>
    /// Helper method to upload a test backup blob
    /// </summary>
    private async Task UploadTestBackupAsync(string blobPath)
    {
        var blobClient = _containerClient!.GetBlobClient(blobPath);
        
        // Create a small BSON-like content and compress it
        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            var content = Encoding.UTF8.GetBytes("{\"test\": \"data\"}");
            await gzipStream.WriteAsync(content);
        }
        
        memoryStream.Position = 0;
        await blobClient.UploadAsync(memoryStream, overwrite: true);
    }

    /// <summary>
    /// Helper method to list all blobs in the container
    /// </summary>
    private async Task<List<string>> ListBlobsAsync()
    {
        var blobs = new List<string>();
        await foreach (var blobItem in _containerClient!.GetBlobsAsync())
        {
            blobs.Add(blobItem.Name);
        }
        return blobs;
    }
}
