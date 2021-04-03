using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OK.Bitter.Engine.Managers
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionManager(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public List<SubscriptionModel> GetSubscriptions()
        {
            return _subscriptionRepository
                .GetList()
                .Select(x => new SubscriptionModel()
                {
                    UserId = x.UserId,
                    SymbolId = x.SymbolId,
                    MinimumChange = x.MinimumChange
                })
                .ToList();
        }

        public List<SubscriptionModel> GetSubscriptionsByUser(string userId)
        {
            return _subscriptionRepository.GetList(x => x.UserId == userId)
                .Select(x => new SubscriptionModel()
                {
                    UserId = x.UserId,
                    SymbolId = x.SymbolId,
                    MinimumChange = x.MinimumChange
                })
                .ToList();
        }

        public bool UpdateAsNotified(string userId, string symbolId, decimal price, DateTime date)
        {
            var subscription = _subscriptionRepository.Get(x => x.UserId == userId && x.SymbolId == symbolId);

            if (subscription == null)
            {
                return false;
            }

            subscription.LastNotifiedPrice = price;
            subscription.LastNotifiedDate = date;

            _subscriptionRepository.Save(subscription);

            return true;
        }
    }
}