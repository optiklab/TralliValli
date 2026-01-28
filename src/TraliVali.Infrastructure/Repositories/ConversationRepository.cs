using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;

namespace TraliVali.Infrastructure.Repositories;

/// <summary>
/// Repository for Conversation entities
/// </summary>
public class ConversationRepository : MongoRepository<Conversation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationRepository"/> class
    /// </summary>
    /// <param name="context">The MongoDB context</param>
    public ConversationRepository(MongoDbContext context) : base(context.Conversations)
    {
    }
}
