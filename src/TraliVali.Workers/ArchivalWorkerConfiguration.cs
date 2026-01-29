namespace TraliVali.Workers;

/// <summary>
/// Configuration for ArchivalWorker
/// </summary>
public class ArchivalWorkerConfiguration
{
    /// <summary>
    /// Gets or sets the cron schedule for the archival worker (default: daily at 2 AM)
    /// </summary>
    public string CronSchedule { get; set; } = "0 2 * * *";

    /// <summary>
    /// Gets or sets the number of days to retain messages before archiving (default: 365)
    /// </summary>
    public int RetentionDays { get; set; } = 365;

    /// <summary>
    /// Gets or sets the batch size for processing messages (default: 1000)
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the Azure Blob Storage connection string
    /// </summary>
    public string BlobStorageConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure Blob Storage container name for archived messages
    /// </summary>
    public string BlobContainerName { get; set; } = "archived-messages";

    /// <summary>
    /// Gets or sets whether to delete messages after successful archival (default: true)
    /// </summary>
    public bool DeleteAfterArchive { get; set; } = true;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold (default: 5)
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the circuit breaker timeout in seconds (default: 30)
    /// </summary>
    public int CircuitBreakerTimeoutSeconds { get; set; } = 30;
}
