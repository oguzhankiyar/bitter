using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;

namespace OK.Bitter.DataAccess.Repositories
{
    public class SubscriptionRepository : BaseRepository<SubscriptionEntity>, ISubscriptionRepository
    {
        public SubscriptionRepository(BitterDataContext context) : base(context.Subscriptions)
        {

        }
    }
}