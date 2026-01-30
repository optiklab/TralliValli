using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Infrastructure.Repositories;

/// <summary>
/// Repository for UserKeyBackup entities
/// </summary>
public class UserKeyBackupRepository : MongoRepository<UserKeyBackup>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserKeyBackupRepository"/> class
    /// </summary>
    /// <param name="context">The MongoDB context</param>
    public UserKeyBackupRepository(MongoDbContext context) : base(context.UserKeyBackups)
    {
    }
}
