using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Extensions;
using OK.Bitter.Engine.Stores;

namespace OK.Bitter.Engine.Calculations
{
    public class PriceChangeCalculation
    {
        private readonly IUserManager _userManager;
        private readonly IPriceManager _priceManager;
        private readonly IStore<PriceModel> _priceStore;
        private readonly ISubscriptionManager _subscriptionManager;

        private SymbolModel _symbol;
        private List<SubscriptionModel> _subscriptions;
        private IDictionary<string, (decimal Price, bool IsReverse)> _routePrices;

        public PriceChangeCalculation(
            IUserManager userManager,
            IPriceManager priceManager,
            IStore<PriceModel> priceStore,
            ISubscriptionManager subscriptionManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _priceManager = priceManager ?? throw new ArgumentNullException(nameof(priceManager));
            _priceStore = priceStore ?? throw new ArgumentNullException(nameof(priceStore));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        public Task InitAsync(SymbolModel symbol)
        {
            _symbol = symbol;
            _subscriptions = new List<SubscriptionModel>();
            _routePrices = new Dictionary<string, (decimal Price, bool IsReverse)>();

            foreach (var item in _symbol.Route)
            {
                _routePrices.Add(string.Concat(item.Base, item.Quote), (0, item.IsReverse));
            }

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

        public Task CalculateAsync(string symbol, DateTime date, decimal price)
        {
            if (_routePrices.TryGetValue(symbol, out var routePrice))
            {
                _routePrices[symbol] = (price, routePrice.IsReverse);
            }

            if (_routePrices.Any(x => x.Value.Price == decimal.Zero))
            {
                return Task.CompletedTask;
            }

            var symbolPrice = 1m;

            foreach (var item in _routePrices)
            {
                symbolPrice *= item.Value.IsReverse ? (1 / item.Value.Price) : item.Value.Price;
            }

            var last = _priceStore.Find(x => x.SymbolId == _symbol.Id);
            if (last == null || last.Price == decimal.Zero)
            {
                _priceStore.Upsert(new PriceModel
                {
                    SymbolId = _symbol.Id,
                    Price = symbolPrice,
                    Date = date
                });
                _priceManager.SaveLastPrice(_symbol.Id, symbolPrice, decimal.Zero, date, DateTime.UtcNow);

                return Task.CompletedTask;
            }

            var change = Math.Abs((symbolPrice - last.Price) / last.Price);

            if (change < _symbol.MinimumChange)
            {
                return Task.CompletedTask;
            }

            foreach (var subscription in _subscriptions)
            {
                if (subscription.LastNotifiedPrice == decimal.Zero)
                {
                    subscription.LastNotifiedPrice = symbolPrice;
                    subscription.LastNotifiedDate = DateTime.UtcNow;
                }
                else
                {
                    var userPrice = subscription.LastNotifiedPrice;
                    var userChange = (symbolPrice - userPrice) / userPrice;

                    if (Math.Abs(userChange) >= subscription.MinimumChange)
                    {
                        var message = string.Format("{0}: {1} {2} [{3}% {4}]",
                            _symbol.Base,
                            symbolPrice.ToString("0.00######"),
                            _symbol.Quote,
                            (userChange * 100).ToString("+0.00;-0.00;0"),
                            (DateTime.UtcNow - subscription.LastNotifiedDate).ToIntervalString());

                        _userManager.SendMessage(subscription.UserId, message);

                        subscription.LastNotifiedPrice = symbolPrice;
                        subscription.LastNotifiedDate = DateTime.UtcNow;

                        _subscriptionManager.UpdateAsNotified(subscription.UserId, _symbol.Id, symbolPrice, DateTime.UtcNow);
                    }
                }
            }

            _priceStore.Upsert(new PriceModel
            {
                SymbolId = _symbol.Id,
                Price = symbolPrice,
                Date = date
            });
            _priceManager.SaveLastPrice(_symbol.Id, symbolPrice, change, date, DateTime.UtcNow);

            return Task.CompletedTask;
        }
    }
}