using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace TraliVali.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation for managing conversation message archives
/// </summary>
public class AzureBlobService : IAzureBlobService
{
    private readonly BlobContainerClient _containerClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobService"/> class
    /// </summary>
    /// <param name="connectionString">Azure Blob Storage connection string</param>
    /// <param name="containerName">The name of the blob container</param>
    public AzureBlobService(string connectionString, string containerName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobService"/> class with a container client
    /// </summary>
    /// <param name="containerClient">The blob container client</param>
    public AzureBlobService(BlobContainerClient containerClient)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
    }

    /// <summary>
    /// Ensures the blob container exists
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UploadArchiveAsync(Stream stream, string path, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        var blobClient = _containerClient.GetBlobClient(path);
        
        // Reset stream position if seekable
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadArchiveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        var blobClient = _containerClient.GetBlobClient(path);

        try
        {
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException($"Archive not found at path: {path}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> ListArchivesAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var archives = new List<string>();

        await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            archives.Add(blobItem.Name);
        }

        return archives;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteArchiveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        var blobClient = _containerClient.GetBlobClient(path);
        
        try
        {
            var response = await blobClient.DeleteAsync(cancellationToken: cancellationToken);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    /// <summary>
    /// Generates the archive path for a conversation based on the specified date
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="date">The date for the archive</param>
    /// <returns>The formatted archive path</returns>
    public static string GenerateArchivePath(string conversationId, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));

        var dateString = date.ToString("yyyy-MM-dd");
        return $"archives/{date.Year:D4}/{date.Month:D2}/messages_{conversationId}_{dateString}.json";
    }

    /// <inheritdoc/>
    public string GenerateUploadUrl(string blobPath, TimeSpan? expiresIn = null)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
            throw new ArgumentException("Blob path cannot be null or empty", nameof(blobPath));

        var blobClient = _containerClient.GetBlobClient(blobPath);
        var expiresOn = DateTimeOffset.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(1));

        // Generate SAS token with write permissions
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobPath,
            Resource = "b", // blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow 5 minutes clock skew
            ExpiresOn = expiresOn
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri.ToString();
    }

    /// <inheritdoc/>
    public string GenerateDownloadUrl(string blobPath, TimeSpan? expiresIn = null)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
            throw new ArgumentException("Blob path cannot be null or empty", nameof(blobPath));

        var blobClient = _containerClient.GetBlobClient(blobPath);
        var expiresOn = DateTimeOffset.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(1));

        // Generate SAS token with read permissions
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobPath,
            Resource = "b", // blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow 5 minutes clock skew
            ExpiresOn = expiresOn
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri.ToString();
    }
}
