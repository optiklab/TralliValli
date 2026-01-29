namespace TraliVali.Workers;

/// <summary>
/// Configuration for BackupWorker
/// </summary>
public class BackupWorkerConfiguration
{
    /// <summary>
    /// Gets or sets the cron schedule for the backup worker (default: daily at 3 AM)
    /// </summary>
    public string CronSchedule { get; set; } = "0 3 * * *";

    /// <summary>
    /// Gets or sets the Azure Blob Storage connection string
    /// </summary>
    public string BlobStorageConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure Blob Storage container name for backups
    /// </summary>
    public string BlobContainerName { get; set; } = "backups";

    /// <summary>
    /// Gets or sets the number of days to retain backups (default: 30)
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold (default: 5)
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the circuit breaker timeout in seconds (default: 30)
    /// </summary>
    public int CircuitBreakerTimeoutSeconds { get; set; } = 30;
}
