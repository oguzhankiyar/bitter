using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Calculations;
using OK.Bitter.Engine.Stores;
using OK.Bitter.Engine.Streams;

namespace OK.Bitter.Engine.Managers
{
    public class SocketManager : ISocketManager
    {
        private readonly IStore<SymbolModel> _symbolStore;
        private readonly IStore<SubscriptionModel> _subscriptionStore;
        private readonly IStore<AlertModel> _alertStore;
        private readonly IServiceProvider _serviceProvider;

        private readonly IDictionary<string, List<string>> _symbolMap;
        private readonly IDictionary<string, IPriceStream> _streams;
        private readonly IDictionary<string, PriceChangeCalculation> _priceChangeCalculations;
        private readonly IDictionary<string, PriceAlertCalculation> _priceAlertCalculations;

        public SocketManager(
            IStore<SymbolModel> symbolStore,
            IStore<SubscriptionModel> subscriptionStore,
            IStore<AlertModel> alertStore,
            IServiceProvider serviceProvider)
        {
            _symbolStore = symbolStore ?? throw new ArgumentNullException(nameof(symbolStore));
            _subscriptionStore = subscriptionStore ?? throw new ArgumentNullException(nameof(subscriptionStore));
            _alertStore = alertStore ?? throw new ArgumentNullException(nameof(alertStore));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _symbolMap = new Dictionary<string, List<string>>();
            _streams = new Dictionary<string, IPriceStream>();
            _priceChangeCalculations = new ConcurrentDictionary<string, PriceChangeCalculation>();
            _priceAlertCalculations = new ConcurrentDictionary<string, PriceAlertCalculation>();

            _symbolStore.OnInserted += (_, symbol) => ReloadSymbolMap();
            _symbolStore.OnUpdated += (_, symbol) => ReloadSymbolMap();
            _symbolStore.OnDeleted += (_, symbol) => ReloadSymbolMap();

            _subscriptionStore.OnInserted += (_, subscription) => AddOrUpdateSubscription(subscription).ConfigureAwait(false);
            _subscriptionStore.OnUpdated += (_, subscription) => AddOrUpdateSubscription(subscription).ConfigureAwait(false);
            _subscriptionStore.OnDeleted += (_, subscription) => DeleteSubscription(subscription).ConfigureAwait(false);

            _alertStore.OnInserted += (_, alert) => AddOrUpdateAlert(alert).ConfigureAwait(false);
            _alertStore.OnUpdated += (_, alert) => AddOrUpdateAlert(alert).ConfigureAwait(false);
            _alertStore.OnDeleted += (_, alert) => DeleteAlert(alert).ConfigureAwait(false);
        }

        public async Task SubscribeAsync()
        {
            var streamSymbols = new List<string>();
            var symbols = _symbolStore.Get();

            foreach (var symbol in symbols)
            {
                if (!_priceChangeCalculations.ContainsKey(string.Concat(symbol.Base, symbol.Quote)))
                {
                    var calculation = _serviceProvider.GetRequiredService<PriceChangeCalculation>();
                    await calculation.InitAsync(symbol);
                    _priceChangeCalculations.Add(string.Concat(symbol.Base, symbol.Quote), calculation);
                }

                foreach (var routeSymbol in symbol.Route)
                {
                    var symbolName = string.Concat(routeSymbol.Base, routeSymbol.Quote);

                    if (!streamSymbols.Contains(symbolName))
                    {
                        streamSymbols.Add(symbolName);
                    }
                }
            }

            foreach (var streamSymbol in streamSymbols)
            {
                if (_streams.ContainsKey(streamSymbol))
                {
                    continue;
                }

                var stream = _serviceProvider.GetRequiredService<IPriceStream>();

                await stream.InitAsync(streamSymbol);
                await stream.SubscribeAsync((_, price) => CalculatePriceChange(price).ConfigureAwait(false));
                await stream.SubscribeAsync((_, price) => CalculatePriceAlert(price).ConfigureAwait(false));
                await stream.StartAsync();

                _streams.Add(streamSymbol, stream);
            }
        }

        public Task UnsubscribeAsync()
        {
            var tasks = new List<Task>();

            foreach (var stream in _streams.Values)
            {
                tasks.Add(stream.StopAsync());
            }

            return Task.WhenAll(tasks);
        }

        public void ResetCache(string userId, string symbolId = null)
        {
            var subscriptions = _subscriptionStore.Get(x => x.UserId == userId);

            if (!string.IsNullOrEmpty(symbolId))
            {
                subscriptions = subscriptions.Where(x => x.SymbolId == symbolId).ToList();
            }

            foreach (var subscription in subscriptions)
            {
                subscription.LastNotifiedPrice = decimal.Zero;
                subscription.LastNotifiedDate = DateTime.Now;

                _subscriptionStore.Upsert(subscription);
            }

            var alerts = _alertStore.Get(x => x.UserId == userId);
            
            if (!string.IsNullOrEmpty(symbolId))
            {
                alerts = alerts.Where(x => x.SymbolId == symbolId).ToList();
            }

            foreach (var alert in alerts)
            {
                alert.LastAlertDate = DateTime.Now;
                
                _alertStore.Upsert(alert);
            }
        }

        public List<string> CheckStatus()
        {
            var symbolSockets = _streams;

            var lines = new List<string>();

            foreach (var item in symbolSockets)
            {
                var symbol = _symbolStore.Find(x => x.Id == item.Key);

                lines.Add($"{symbol.FriendlyName} symbol stream is {item.Value.State}");
            }

            return lines;
        }

        public string CheckSymbolStatus(string symbolId)
        {
            var symbol = _symbolStore.Find(x => x.Id == symbolId);
            if (symbol == null)
            {
                return "Symbol is not found!";
            }

            var symbolName = string.Concat(symbol.Base, symbol.Quote);

            if (!_streams.TryGetValue(symbolName, out var stream))
            {
                return "Stream is not found!";
            }

            return $"{symbol.FriendlyName} symbol stream is {stream.State}";
        }

        private async Task CalculatePriceChange(PriceModel price)
        {
            if (!_symbolMap.TryGetValue(price.SymbolId, out var symbols))
            {
                return;
            }

            var tasks = new List<Task>();

            foreach (var symbol in symbols)
            {
                if (_priceChangeCalculations.TryGetValue(symbol, out var calc))
                {
                    tasks.Add(calc.CalculateAsync(price.SymbolId, price.Date, price.Price));
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task CalculatePriceAlert(PriceModel price)
        {
            if (!_symbolMap.TryGetValue(price.SymbolId, out var symbols))
            {
                return;
            }
            
            var tasks = new List<Task>();
            
            foreach (var symbol in symbols)
            {
                if (_priceAlertCalculations.TryGetValue(symbol, out var calc))
                {
                    tasks.Add(calc.CalculateAsync(price.SymbolId, price.Date, price.Price));
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task AddOrUpdateSubscription(SubscriptionModel subscription)
        {
            var symbol = _symbolStore.Find(x => x.Id == subscription.SymbolId);

            if (_priceChangeCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out var existing))
            {
                await existing.SubscribeAsync(subscription);
            }
            else
            {
                var calculation = _serviceProvider.GetRequiredService<PriceChangeCalculation>();
                await calculation.InitAsync(symbol);
                await calculation.SubscribeAsync(subscription);
                _priceChangeCalculations.Add(string.Concat(symbol.Base, symbol.Quote), calculation);
            }
        }
        
        private async Task DeleteSubscription(SubscriptionModel subscription)
        {
            var symbol = _symbolStore.Find(x => x.Id == subscription.SymbolId);

            if (_priceChangeCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out var calculation))
            {
                await calculation.UnsubscribeAsync(subscription);
            }
        }

        private async Task AddOrUpdateAlert(AlertModel alert)
        {
            var symbol = _symbolStore.Find(x => x.Id == alert.SymbolId);

            if (_priceAlertCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out PriceAlertCalculation existing))
            {
                await existing.SubscribeAsync(alert);
            }
            else
            {
                var calculation = _serviceProvider.GetRequiredService<PriceAlertCalculation>();
                await calculation.InitAsync(symbol);
                await calculation.SubscribeAsync(alert);
                _priceAlertCalculations.Add(string.Concat(symbol.Base, symbol.Quote), calculation);
            }
        }

        private async Task DeleteAlert(AlertModel alert)
        {
            var symbol = _symbolStore.Find(x => x.Id == alert.SymbolId);

            if (_priceAlertCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out PriceAlertCalculation calculation))
            {
                await calculation.UnsubscribeAsync(alert);
            }
        }

        private void ReloadSymbolMap()
        {
            var symbols = _symbolStore.Get();
               
            foreach (var symbol in symbols)
            {
                var symbolName = string.Concat(symbol.Base, symbol.Quote);

                foreach (var related in GetRelatedSymbols(symbol))
                {
                    if (_symbolMap.TryGetValue(related, out var items))
                    {
                        if (!items.Contains(symbolName))
                        {
                            items.Add(symbolName);
                        }
                    }
                    else
                    {
                        _symbolMap.Add(related, new List<string> { symbolName });
                    }
                }
            }
        }

        private List<string> GetRelatedSymbols(SymbolModel symbol)
        {
            var symbols = new List<string>();

            foreach (var item in symbol.Route)
            {
                var symbolName = string.Concat(item.Base, item.Quote);

                if (!symbols.Contains(symbolName))
                {
                    symbols.Add(symbolName);
                }
            }

            return symbols;
        }
    }
}