using System;
using System.Collections.Generic;
using System.Linq;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;

namespace OK.Bitter.Engine.Stores
{
    public class SubscriptionStore : IStore<SubscriptionModel>
    {
        public event EventHandler<SubscriptionModel> OnInserted;
        public event EventHandler<SubscriptionModel> OnUpdated;
        public event EventHandler<SubscriptionModel> OnDeleted;

        private readonly ISubscriptionManager _subscriptionManager;

        private List<SubscriptionModel> _items = new List<SubscriptionModel>();

        public SubscriptionStore(ISubscriptionManager subscriptionManager)
        {
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        public void Init()
        {
            _subscriptionManager
                .GetSubscriptions()
                .ForEach(Upsert);
        }

        public List<SubscriptionModel> Get(Func<SubscriptionModel, bool> expression = null)
        {
            if (expression == null)
            {
                return _items;
            }

            return _items.Where(expression).ToList();
        }

        public SubscriptionModel Find(Func<SubscriptionModel, bool> expression)
        {
            return _items.FirstOrDefault(expression);
        }

        public void Upsert(SubscriptionModel subscription)
        {
            var item = _items.FirstOrDefault(x => x.UserId == subscription.UserId && x.SymbolId == subscription.SymbolId);
            if (item != null)
            {
                item.LastNotifiedPrice = subscription.LastNotifiedPrice;
                item.LastNotifiedDate = subscription.LastNotifiedDate;
                item.MinimumChange = subscription.MinimumChange;
                OnUpdated?.Invoke(this, subscription);
            }
            else
            {
                _items.Add(subscription);
                OnInserted?.Invoke(this, subscription);
            }
        }

        public void Delete(SubscriptionModel subscription)
        {
            _items.RemoveAll(x => x.UserId == subscription.UserId && x.SymbolId == subscription.SymbolId);
            OnDeleted?.Invoke(this, subscription);
        }

        public void Delete(Func<SubscriptionModel, bool> filter = null)
        {
            var subscriptions = Get(filter);

            foreach (var subscription in subscriptions)
            {
                _items.RemoveAll(x => x.UserId == subscription.UserId && x.SymbolId == subscription.SymbolId);
                OnDeleted?.Invoke(this, subscription);
            }
        }
    }
}