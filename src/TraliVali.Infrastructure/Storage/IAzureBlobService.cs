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
}
