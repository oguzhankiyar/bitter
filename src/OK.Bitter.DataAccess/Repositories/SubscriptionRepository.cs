using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;
using System.Collections.Generic;

namespace OK.Bitter.DataAccess.Repositories
{
    public class SubscriptionRepository : BaseRepository<SubscriptionEntity>, ISubscriptionRepository
    {
        public SubscriptionRepository(BitterDataContext context) : base(context, "Subscriptions")
        {
        }

        public IEnumerable<SubscriptionEntity> FindSubscriptions()
        {
            return GetList();
        }

        public SubscriptionEntity FindSubscription(string symbolId, string userId)
        {
            return Get(x => x.UserId == userId && x.SymbolId == symbolId);
        }

        public SubscriptionEntity InsertSubscription(SubscriptionEntity subscription)
        {
            return Save(subscription);
        }

        public bool UpdateSubscription(SubscriptionEntity subscription)
        {
            Save(subscription);

            return true;
        }

        public bool RemoveSubscription(string subscriptionId)
        {
            Delete(subscriptionId);

            return true;
        }
    }
}