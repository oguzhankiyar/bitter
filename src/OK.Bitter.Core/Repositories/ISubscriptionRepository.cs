using OK.Bitter.Common.Entities;
using System.Collections.Generic;

namespace OK.Bitter.Core.Repositories
{
    public interface ISubscriptionRepository
    {
        IEnumerable<SubscriptionEntity> FindSubscriptions();

        SubscriptionEntity FindSubscription(string symbolId, string userId);

        SubscriptionEntity InsertSubscription(SubscriptionEntity subscription);

        bool UpdateSubscription(SubscriptionEntity subscription);

        bool RemoveSubscription(string subscriptionId);
    }
}