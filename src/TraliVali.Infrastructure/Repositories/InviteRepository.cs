using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Infrastructure.Repositories;

/// <summary>
/// Repository for Invite entities
/// </summary>
public class InviteRepository : MongoRepository<Invite>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InviteRepository"/> class
    /// </summary>
    /// <param name="context">The MongoDB context</param>
    public InviteRepository(MongoDbContext context) : base(context.Invites)
    {
    }
}
