using OK.Bitter.Common.Models;
using System;
using System.Collections.Generic;

namespace OK.Bitter.Core.Managers
{
    public interface ISubscriptionManager
    {
        List<SubscriptionModel> GetSubscriptions();

        List<SubscriptionModel> GetSubscriptionsByUser(string userId);

        bool UpdateAsNotified(string userId, string symbolId, decimal price, DateTime date);
    }
}