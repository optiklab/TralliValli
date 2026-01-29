using TraliVali.Domain.Entities;

namespace TraliVali.Auth;

/// <summary>
/// Service for managing database backups
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Triggers a manual backup operation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created backup record</returns>
    Task<Backup> TriggerBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available backups
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of backups</returns>
    Task<IEnumerable<Backup>> ListBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the database from a backup
    /// </summary>
    /// <param name="date">The date of the backup to restore (yyyy-MM-dd format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if restore was successful</returns>
    Task<bool> RestoreBackupAsync(string date, CancellationToken cancellationToken = default);
}
