namespace TraliVali.Workers.Models;

/// <summary>
/// Represents the payload of a file processing message in the queue
/// </summary>
public class FileQueuePayload
{
    /// <summary>
    /// Gets or sets the file identifier
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blob path where the file is stored
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type of the file
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
