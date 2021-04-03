using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;

namespace OK.Bitter.DataAccess.Repositories
{
    public class MessageRepository : BaseRepository<MessageEntity>, IMessageRepository
    {
        public MessageRepository(BitterDataContext context) : base(context.Messages)
        {

        }
    }
}