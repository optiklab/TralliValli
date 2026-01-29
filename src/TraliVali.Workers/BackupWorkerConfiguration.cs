namespace TraliVali.Workers;

/// <summary>
/// Configuration for BackupWorker
/// </summary>
public class BackupWorkerConfiguration
{
    private int _retentionDays = 30;
    private int _circuitBreakerFailureThreshold = 5;
    private int _circuitBreakerTimeoutSeconds = 30;

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
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0</exception>
    public int RetentionDays
    {
        get => _retentionDays;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(RetentionDays), "RetentionDays must be greater than 0");
            _retentionDays = value;
        }
    }

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold (default: 5)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0</exception>
    public int CircuitBreakerFailureThreshold
    {
        get => _circuitBreakerFailureThreshold;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(CircuitBreakerFailureThreshold), "CircuitBreakerFailureThreshold must be greater than 0");
            _circuitBreakerFailureThreshold = value;
        }
    }

    /// <summary>
    /// Gets or sets the circuit breaker timeout in seconds (default: 30)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0</exception>
    public int CircuitBreakerTimeoutSeconds
    {
        get => _circuitBreakerTimeoutSeconds;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(CircuitBreakerTimeoutSeconds), "CircuitBreakerTimeoutSeconds must be greater than 0");
            _circuitBreakerTimeoutSeconds = value;
        }
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BlobStorageConnectionString))
            throw new InvalidOperationException("BlobStorageConnectionString is required");

        if (string.IsNullOrWhiteSpace(BlobContainerName))
            throw new InvalidOperationException("BlobContainerName is required");

        if (string.IsNullOrWhiteSpace(CronSchedule))
            throw new InvalidOperationException("CronSchedule is required");
    }
}
