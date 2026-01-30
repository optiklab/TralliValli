using System.ComponentModel.DataAnnotations;

namespace TraliVali.Api.Models;

/// <summary>
/// Request model for generating presigned upload URL
/// </summary>
public class GenerateUploadUrlRequest
{
    /// <summary>
    /// Gets or sets the conversation ID
    /// </summary>
    [Required]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type
    /// </summary>
    [Required]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes
    /// </summary>
    [Required]
    [Range(1, 104857600)] // Max 100MB
    public long Size { get; set; }
}

/// <summary>
/// Response model for presigned upload URL
/// </summary>
public class GenerateUploadUrlResponse
{
    /// <summary>
    /// Gets or sets the file ID
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the presigned upload URL
    /// </summary>
    public string UploadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blob path
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Request model for confirming file upload completion
/// </summary>
public class CompleteUploadRequest
{
    /// <summary>
    /// Gets or sets the file ID
    /// </summary>
    [Required]
    public string FileId { get; set; } = string.Empty;
}

/// <summary>
/// Response model for file completion
/// </summary>
public class CompleteUploadResponse
{
    /// <summary>
    /// Gets or sets the file ID
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a message indicating success
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response model for presigned download URL
/// </summary>
public class GenerateDownloadUrlResponse
{
    /// <summary>
    /// Gets or sets the file ID
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the presigned download URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
