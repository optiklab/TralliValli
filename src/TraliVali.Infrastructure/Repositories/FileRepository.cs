using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Infrastructure.Repositories;

/// <summary>
/// Repository for File entities
/// </summary>
public class FileRepository : MongoRepository<Domain.Entities.File>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileRepository"/> class
    /// </summary>
    /// <param name="context">The MongoDB context</param>
    public FileRepository(MongoDbContext context) : base(context.Files)
    {
    }
}
