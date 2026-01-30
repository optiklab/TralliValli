namespace TraliVali.Infrastructure.Storage;

/// <summary>
/// Service for managing conversation message archives in Azure Blob Storage
/// </summary>
public interface IAzureBlobService
{
    /// <summary>
    /// Uploads an archive to Azure Blob Storage
    /// </summary>
    /// <param name="stream">The stream containing the archive data</param>
    /// <param name="path">The blob path where the archive will be stored</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task UploadArchiveAsync(Stream stream, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an archive from Azure Blob Storage
    /// </summary>
    /// <param name="path">The blob path of the archive to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A stream containing the archive data</returns>
    /// <remarks>
    /// The caller is responsible for disposing the returned stream when finished.
    /// </remarks>
    Task<Stream> DownloadArchiveAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all archives matching the specified prefix
    /// </summary>
    /// <param name="prefix">The prefix to filter archives (e.g., "archives/2024/01/")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of archive paths</returns>
    Task<IEnumerable<string>> ListArchivesAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an archive from Azure Blob Storage
    /// </summary>
    /// <param name="path">The blob path of the archive to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the archive was deleted, false if it did not exist</returns>
    Task<bool> DeleteArchiveAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned upload URL with SAS token for direct file upload
    /// </summary>
    /// <param name="blobPath">The blob path where the file will be uploaded</param>
    /// <param name="expiresIn">Time until the URL expires (default: 1 hour)</param>
    /// <returns>The presigned upload URL</returns>
    string GenerateUploadUrl(string blobPath, TimeSpan? expiresIn = null);

    /// <summary>
    /// Generates a presigned download URL with SAS token for file download
    /// </summary>
    /// <param name="blobPath">The blob path of the file to download</param>
    /// <param name="expiresIn">Time until the URL expires (default: 1 hour)</param>
    /// <returns>The presigned download URL</returns>
    string GenerateDownloadUrl(string blobPath, TimeSpan? expiresIn = null);

    /// <summary>
    /// Ensures the blob container exists
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default);
}
