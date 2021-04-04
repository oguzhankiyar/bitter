using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Extensions;

namespace OK.Bitter.Engine.Calculations
{
    public class PriceChangeCalculation
    {
        private readonly IUserManager _userManager;
        private readonly IPriceManager _priceManager;
        private readonly ISubscriptionManager _subscriptionManager;

        private SymbolModel _symbol;
        private decimal _lastPrice;
        private DateTime _lastDate;
        private List<SubscriptionModel> _subscriptions;

        public PriceChangeCalculation(
            IUserManager userManager,
            IPriceManager priceManager,
            ISubscriptionManager subscriptionManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _priceManager = priceManager ?? throw new ArgumentNullException(nameof(priceManager));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        public Task InitAsync(SymbolModel symbol)
        {
            _symbol = symbol;
            _lastPrice = decimal.Zero;
            _lastDate = DateTime.UtcNow;
            _subscriptions = new List<SubscriptionModel>();

            return Task.CompletedTask;
        }

        public Task SubscribeAsync(SubscriptionModel subscription)
        {
            _subscriptions.Add(subscription);

            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(SubscriptionModel subscription)
        {
            var current = _subscriptions.First(x => x.UserId == subscription.UserId && x.SymbolId == subscription.SymbolId);

            if (current != null)
            {
                _subscriptions.Remove(current);
            }

            return Task.CompletedTask;
        }

        public Task CalculateAsync(PriceModel price)
        {
            if (_lastPrice == decimal.Zero)
            {
                _lastPrice = price.Price;
                _lastDate = price.Date;

                return Task.CompletedTask;
            }

            var change = Math.Abs((price.Price - _lastPrice) / _lastPrice);

            if (change < _symbol.MinimumChange)
            {
                return Task.CompletedTask;
            }

            foreach (var subscription in _subscriptions)
            {
                if (subscription.LastNotifiedPrice == decimal.Zero)
                {
                    subscription.LastNotifiedPrice = price.Price;
                    subscription.LastNotifiedDate = DateTime.UtcNow;
                }
                else
                {
                    var userPrice = subscription.LastNotifiedPrice;
                    var userChange = (price.Price - userPrice) / userPrice;

                    if (Math.Abs(userChange) >= subscription.MinimumChange)
                    {
                        var message = string.Format("{0}: {1} [{2}% {3}]",
                            _symbol.FriendlyName,
                            price.Price,
                            (userChange * 100).ToString("+0.00;-0.00;0"),
                            (DateTime.UtcNow - subscription.LastNotifiedDate).ToIntervalString());

                        _userManager.SendMessage(subscription.UserId, message);

                        subscription.LastNotifiedPrice = price.Price;
                        subscription.LastNotifiedDate = DateTime.UtcNow;

                        _subscriptionManager.UpdateAsNotified(subscription.UserId, _symbol.Id, price.Price, DateTime.UtcNow);
                    }
                }
            }

            _priceManager.SaveLastPrice(_symbol.Id, price.Price, change, _lastDate, DateTime.UtcNow);

            _lastPrice = price.Price;
            _lastDate = price.Date;

            return Task.CompletedTask;
        }
    }
}