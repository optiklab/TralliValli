using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using FFMpegCore;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Messaging;
using TraliVali.Infrastructure.Storage;
using TraliVali.Workers.Models;

namespace TraliVali.Workers;

/// <summary>
/// Configuration for FileProcessorWorker
/// </summary>
public class FileProcessorWorkerConfiguration
{
    /// <summary>
    /// Gets or sets the dead-letter queue name
    /// </summary>
    public string DeadLetterQueueName { get; set; } = "files.process.deadletter";

    /// <summary>
    /// Gets or sets the maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum thumbnail dimension in pixels
    /// </summary>
    public int ThumbnailMaxDimension { get; set; } = 300;
}

/// <summary>
/// Background worker that processes files from the file queue
/// </summary>
public class FileProcessorWorker : BackgroundService
{
    private const string QueueName = "files.process";
    private readonly IMessageConsumer _messageConsumer;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IMongoCollection<Domain.Entities.File> _filesCollection;
    private readonly IAzureBlobService _blobService;
    private readonly ILogger<FileProcessorWorker> _logger;
    private readonly FileProcessorWorkerConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessorWorker"/> class
    /// </summary>
    /// <param name="messageConsumer">The message consumer</param>
    /// <param name="messagePublisher">The message publisher</param>
    /// <param name="filesCollection">The files collection</param>
    /// <param name="blobService">The Azure blob service</param>
    /// <param name="configuration">The worker configuration</param>
    /// <param name="logger">The logger instance</param>
    public FileProcessorWorker(
        IMessageConsumer messageConsumer,
        IMessagePublisher messagePublisher,
        IMongoCollection<Domain.Entities.File> filesCollection,
        IAzureBlobService blobService,
        FileProcessorWorkerConfiguration configuration,
        ILogger<FileProcessorWorker> logger)
    {
        _messageConsumer = messageConsumer ?? throw new ArgumentNullException(nameof(messageConsumer));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _filesCollection = filesCollection ?? throw new ArgumentNullException(nameof(filesCollection));
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the worker
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileProcessorWorker starting...");

        try
        {
            // Start consuming messages
            await _messageConsumer.StartConsumingAsync(QueueName, ProcessFileAsync, stoppingToken);

            _logger.LogInformation("FileProcessorWorker started successfully and consuming from {QueueName}", QueueName);

            // Keep the worker running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("FileProcessorWorker stopping due to cancellation request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in FileProcessorWorker");
            throw;
        }
    }

    /// <summary>
    /// Processes a file from the queue
    /// </summary>
    /// <param name="messageJson">The message JSON payload</param>
    private async Task ProcessFileAsync(string messageJson)
    {
        try
        {
            _logger.LogDebug("Processing file message: {MessageJson}", messageJson);

            // Deserialize the message payload
            var payload = JsonSerializer.Deserialize<FileQueuePayload>(messageJson);
            if (payload == null)
            {
                _logger.LogError("Failed to deserialize file payload");
                await SendToDeadLetterQueueAsync(messageJson, "Failed to deserialize payload");
                return;
            }

            // Validate payload
            if (string.IsNullOrWhiteSpace(payload.FileId) || string.IsNullOrWhiteSpace(payload.BlobPath))
            {
                _logger.LogError("Invalid file payload: FileId or BlobPath is missing");
                await SendToDeadLetterQueueAsync(messageJson, "Invalid payload: missing FileId or BlobPath");
                return;
            }

            // Process the file with retry logic for transient failures
            await ProcessFileWithRetryAsync(payload);

            _logger.LogInformation("Successfully processed file {FileId}", payload.FileId);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize file message");
            await SendToDeadLetterQueueAsync(messageJson, $"Deserialization error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing file message");
            await SendToDeadLetterQueueAsync(messageJson, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes a file with retry logic for transient failures
    /// </summary>
    /// <param name="payload">The file payload to process</param>
    private async Task ProcessFileWithRetryAsync(FileQueuePayload payload)
    {
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount < _configuration.MaxRetryAttempts)
        {
            try
            {
                // Get the file record
                var file = await _filesCollection
                    .Find(f => f.Id == payload.FileId)
                    .FirstOrDefaultAsync();

                if (file == null)
                {
                    _logger.LogWarning("File {FileId} not found in database", payload.FileId);
                    return;
                }

                // Check if file is an image or video
                var isImage = payload.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
                var isVideo = payload.MimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

                if (isImage)
                {
                    await ProcessImageAsync(file, payload);
                }
                else if (isVideo)
                {
                    await ProcessVideoAsync(file, payload);
                }
                else
                {
                    _logger.LogInformation("File {FileId} is neither image nor video, skipping processing", payload.FileId);
                }

                return; // Success - exit the retry loop
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                _logger.LogWarning(ex, "Error processing file (attempt {RetryCount}/{MaxRetries})",
                    retryCount, _configuration.MaxRetryAttempts);

                if (retryCount < _configuration.MaxRetryAttempts)
                {
                    // Wait before retrying (exponential backoff)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                }
            }
        }

        // If we get here, all retries failed
        _logger.LogError(lastException, "Failed to process file after {MaxRetries} attempts", _configuration.MaxRetryAttempts);
        var messageJson = JsonSerializer.Serialize(payload);
        await SendToDeadLetterQueueAsync(messageJson, $"Processing failed after {_configuration.MaxRetryAttempts} attempts: {lastException?.Message}");
    }

    /// <summary>
    /// Processes an image file: generates thumbnail, extracts EXIF, stores dimensions
    /// </summary>
    /// <param name="file">The file entity</param>
    /// <param name="payload">The file payload</param>
    private async Task ProcessImageAsync(Domain.Entities.File file, FileQueuePayload payload)
    {
        _logger.LogInformation("Processing image file {FileId}", payload.FileId);

        // Download the image from blob storage
        using var imageStream = await _blobService.DownloadArchiveAsync(payload.BlobPath);
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        // Load the image
        using var image = await Image.LoadAsync(memoryStream);

        // Store dimensions
        file.Width = image.Width;
        file.Height = image.Height;

        // Extract EXIF metadata
        var exifProfile = image.Metadata.ExifProfile;
        if (exifProfile != null)
        {
            var exifData = new Dictionary<string, string>();

            // Extract common EXIF tags
            AddExifValue(exifData, exifProfile, ExifTag.Make, "Make");
            AddExifValue(exifData, exifProfile, ExifTag.Model, "Model");
            AddExifValue(exifData, exifProfile, ExifTag.DateTime, "DateTime");
            AddExifValue(exifData, exifProfile, ExifTag.ExposureTime, "ExposureTime");
            AddExifValue(exifData, exifProfile, ExifTag.FNumber, "FNumber");
            AddExifValue(exifData, exifProfile, ExifTag.ISOSpeedRatings, "ISO");
            AddExifValue(exifData, exifProfile, ExifTag.FocalLength, "FocalLength");

            file.ExifData = JsonSerializer.Serialize(exifData);
        }

        // Generate thumbnail
        var thumbnailPath = await GenerateThumbnailAsync(image, payload);
        file.ThumbnailPath = thumbnailPath;

        // Update the file record
        var filter = Builders<Domain.Entities.File>.Filter.Eq(f => f.Id, file.Id);
        var update = Builders<Domain.Entities.File>.Update
            .Set(f => f.Width, file.Width)
            .Set(f => f.Height, file.Height)
            .Set(f => f.ExifData, file.ExifData)
            .Set(f => f.ThumbnailPath, file.ThumbnailPath);

        await _filesCollection.UpdateOneAsync(filter, update);

        _logger.LogInformation("Image {FileId} processed: dimensions={Width}x{Height}, thumbnail={ThumbnailPath}",
            payload.FileId, file.Width, file.Height, file.ThumbnailPath);
    }

    /// <summary>
    /// Processes a video file: extracts first frame as thumbnail, stores duration
    /// </summary>
    /// <param name="file">The file entity</param>
    /// <param name="payload">The file payload</param>
    private async Task ProcessVideoAsync(Domain.Entities.File file, FileQueuePayload payload)
    {
        _logger.LogInformation("Processing video file {FileId}", payload.FileId);

        // Download the video from blob storage to a temporary file
        var tempVideoPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(payload.FileName)}");
        try
        {
            using (var videoStream = await _blobService.DownloadArchiveAsync(payload.BlobPath))
            using (var fileStream = System.IO.File.Create(tempVideoPath))
            {
                await videoStream.CopyToAsync(fileStream);
            }

            // Get video information
            var videoInfo = await FFProbe.AnalyseAsync(tempVideoPath);

            // Store duration
            file.Duration = videoInfo.Duration.TotalSeconds;

            // Store video dimensions
            file.Width = videoInfo.PrimaryVideoStream?.Width;
            file.Height = videoInfo.PrimaryVideoStream?.Height;

            // Generate thumbnail from first frame
            var thumbnailPath = await GenerateVideoThumbnailAsync(tempVideoPath, payload);
            file.ThumbnailPath = thumbnailPath;

            // Update the file record
            var filter = Builders<Domain.Entities.File>.Filter.Eq(f => f.Id, file.Id);
            var update = Builders<Domain.Entities.File>.Update
                .Set(f => f.Duration, file.Duration)
                .Set(f => f.Width, file.Width)
                .Set(f => f.Height, file.Height)
                .Set(f => f.ThumbnailPath, file.ThumbnailPath);

            await _filesCollection.UpdateOneAsync(filter, update);

            _logger.LogInformation("Video {FileId} processed: duration={Duration}s, dimensions={Width}x{Height}, thumbnail={ThumbnailPath}",
                payload.FileId, file.Duration, file.Width, file.Height, file.ThumbnailPath);
        }
        finally
        {
            // Clean up temporary video file
            if (System.IO.File.Exists(tempVideoPath))
            {
                System.IO.File.Delete(tempVideoPath);
            }
        }
    }

    /// <summary>
    /// Generates a thumbnail for an image
    /// </summary>
    /// <param name="image">The source image</param>
    /// <param name="payload">The file payload</param>
    /// <returns>The blob path of the generated thumbnail</returns>
    private async Task<string> GenerateThumbnailAsync(Image image, FileQueuePayload payload)
    {
        // Calculate thumbnail dimensions maintaining aspect ratio
        var maxDimension = _configuration.ThumbnailMaxDimension;
        var scale = Math.Min(maxDimension / (double)image.Width, maxDimension / (double)image.Height);
        
        if (scale >= 1)
        {
            // Image is already smaller than thumbnail size, use original
            scale = 1;
        }

        var thumbnailWidth = (int)(image.Width * scale);
        var thumbnailHeight = (int)(image.Height * scale);

        // Create thumbnail
        using var thumbnail = image.Clone(ctx => ctx.Resize(thumbnailWidth, thumbnailHeight));
        
        // Generate thumbnail blob path
        var fileIdWithoutExtension = Path.GetFileNameWithoutExtension(payload.FileId);
        var thumbnailPath = $"thumbnails/{fileIdWithoutExtension}_thumb.jpg";
        
        // Upload thumbnail to blob storage
        using var thumbnailStream = new MemoryStream();
        await thumbnail.SaveAsJpegAsync(thumbnailStream);
        thumbnailStream.Position = 0;
        await _blobService.UploadArchiveAsync(thumbnailStream, thumbnailPath);

        return thumbnailPath;
    }

    /// <summary>
    /// Generates a thumbnail from the first frame of a video
    /// </summary>
    /// <param name="videoPath">Path to the video file</param>
    /// <param name="payload">The file payload</param>
    /// <returns>The blob path of the generated thumbnail</returns>
    private async Task<string> GenerateVideoThumbnailAsync(string videoPath, FileQueuePayload payload)
    {
        var tempThumbnailPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
        try
        {
            // Extract first frame as thumbnail
            await FFMpeg.SnapshotAsync(videoPath, tempThumbnailPath, captureTime: TimeSpan.FromSeconds(0));

            // Resize thumbnail if needed
            using var thumbnailImage = await Image.LoadAsync(tempThumbnailPath);
            var maxDimension = _configuration.ThumbnailMaxDimension;
            var scale = Math.Min(maxDimension / (double)thumbnailImage.Width, maxDimension / (double)thumbnailImage.Height);

            if (scale < 1)
            {
                var thumbnailWidth = (int)(thumbnailImage.Width * scale);
                var thumbnailHeight = (int)(thumbnailImage.Height * scale);
                thumbnailImage.Mutate(ctx => ctx.Resize(thumbnailWidth, thumbnailHeight));
            }

            // Generate thumbnail blob path
            var fileIdWithoutExtension = Path.GetFileNameWithoutExtension(payload.FileId);
            var thumbnailPath = $"thumbnails/{fileIdWithoutExtension}_thumb.jpg";

            // Upload thumbnail to blob storage
            using var thumbnailStream = new MemoryStream();
            await thumbnailImage.SaveAsJpegAsync(thumbnailStream);
            thumbnailStream.Position = 0;
            await _blobService.UploadArchiveAsync(thumbnailStream, thumbnailPath);

            return thumbnailPath;
        }
        finally
        {
            // Clean up temporary thumbnail file
            if (System.IO.File.Exists(tempThumbnailPath))
            {
                System.IO.File.Delete(tempThumbnailPath);
            }
        }
    }

    /// <summary>
    /// Helper method to add EXIF value to dictionary
    /// </summary>
    private void AddExifValue<T>(Dictionary<string, string> exifData, ExifProfile profile, ExifTag<T> tag, string key)
    {
        var value = profile.TryGetValue(tag, out var exifValue);
        if (value && exifValue != null)
        {
            exifData[key] = exifValue.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Sends a failed message to the dead-letter queue
    /// </summary>
    /// <param name="messageJson">The original message JSON</param>
    /// <param name="reason">The reason for failure</param>
    private async Task SendToDeadLetterQueueAsync(string messageJson, string reason)
    {
        try
        {
            var deadLetterPayload = new
            {
                OriginalMessage = messageJson,
                Reason = reason,
                FailedAt = DateTime.UtcNow
            };

            var deadLetterJson = JsonSerializer.Serialize(deadLetterPayload);
            await _messagePublisher.PublishAsync(_configuration.DeadLetterQueueName, deadLetterJson);

            _logger.LogWarning("Sent file message to dead-letter queue: {Reason}", reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send file message to dead-letter queue");
        }
    }

    /// <summary>
    /// Stops the worker and cleans up resources
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FileProcessorWorker stopping...");
        _messageConsumer.StopConsuming();
        await base.StopAsync(cancellationToken);
    }
}
