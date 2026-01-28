using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Infrastructure.Repositories;

/// <summary>
/// Repository for User entities
/// </summary>
public class UserRepository : MongoRepository<User>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class
    /// </summary>
    /// <param name="context">The MongoDB context</param>
    public UserRepository(MongoDbContext context) : base(context.Users)
    {
    }
}
