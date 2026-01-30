using System.Text.Json.Serialization;

namespace TraliVali.Workers.Models;

/// <summary>
/// Represents the payload of a file processing message in the queue
/// </summary>
public class FileQueuePayload
{
    /// <summary>
    /// Gets or sets the file identifier
    /// </summary>
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blob path where the file is stored
    /// </summary>
    [JsonPropertyName("blobPath")]
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type of the file
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;
}
