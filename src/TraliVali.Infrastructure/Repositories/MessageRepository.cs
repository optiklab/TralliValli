using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Infrastructure.Repositories;

/// <summary>
/// Repository for Message entities
/// </summary>
public class MessageRepository : MongoRepository<Message>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRepository"/> class
    /// </summary>
    /// <param name="context">The MongoDB context</param>
    public MessageRepository(MongoDbContext context) : base(context.Messages)
    {
    }
}
