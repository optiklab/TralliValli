using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Infrastructure.Repositories;

/// <summary>
/// Repository for Backup entities
/// </summary>
public class BackupRepository : MongoRepository<Backup>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackupRepository"/> class
    /// </summary>
    /// <param name="context">The MongoDB context</param>
    public BackupRepository(MongoDbContext context) : base(context.Backups)
    {
    }
}
