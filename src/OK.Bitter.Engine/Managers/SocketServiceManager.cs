using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Calculations;
using OK.Bitter.Engine.Stores;
using OK.Bitter.Engine.Streams;

namespace OK.Bitter.Engine.Managers
{
    public class SocketServiceManager : ISocketServiceManager
    {
        private readonly IStore<SymbolModel> _symbolStore;
        private readonly IStore<SubscriptionModel> _subscriptionStore;
        private readonly IStore<AlertModel> _alertStore;
        private readonly IServiceProvider _serviceProvider;

        private readonly IDictionary<string, IPriceStream> _streams;
        private readonly IDictionary<string, PriceChangeCalculation> _priceChangeCalculations;
        private readonly IDictionary<string, PriceAlertCalculation> _priceAlertCalculations;

        public SocketServiceManager(
            IStore<SymbolModel> symbolStore,
            IStore<SubscriptionModel> subscriptionStore,
            IStore<AlertModel> alertStore,
            IServiceProvider serviceProvider)
        {
            _symbolStore = symbolStore ?? throw new ArgumentNullException(nameof(symbolStore));
            _subscriptionStore = subscriptionStore ?? throw new ArgumentNullException(nameof(subscriptionStore));
            _alertStore = alertStore ?? throw new ArgumentNullException(nameof(alertStore));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _streams = new Dictionary<string, IPriceStream>();
            _priceChangeCalculations = new ConcurrentDictionary<string, PriceChangeCalculation>();
            _priceAlertCalculations = new ConcurrentDictionary<string, PriceAlertCalculation>();

            _subscriptionStore.OnInserted += async (_, subscription) =>
            {
                // TODO: Multiple mapping according to route

                var symbol = _symbolStore.Find(x => x.Id == subscription.SymbolId);

                if (_priceChangeCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out PriceChangeCalculation existing))
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
            };

            _subscriptionStore.OnUpdated += async (_, subscription) =>
            {
                // TODO: Multiple mapping according to route

                var symbol = _symbolStore.Find(x => x.Id == subscription.SymbolId);

                if (_priceChangeCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out PriceChangeCalculation existing))
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
            };

            _subscriptionStore.OnDeleted += async (_, subscription) =>
            {
                // TODO: Multiple mapping according to route

                var symbol = _symbolStore.Find(x => x.Id == subscription.SymbolId);

                if (_priceChangeCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out PriceChangeCalculation calculation))
                {
                    await calculation.UnsubscribeAsync(subscription);
                }
            };

            _alertStore.OnInserted += async (_, alert) =>
            {
                // TODO: Multiple mapping according to route

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
            };

            _alertStore.OnUpdated += async (_, alert) =>
            {
                // TODO: Multiple mapping according to route

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
            };

            _alertStore.OnDeleted += async (_, alert) =>
            {
                // TODO: Multiple mapping according to route

                var symbol = _symbolStore.Find(x => x.Id == alert.SymbolId);

                if (_priceAlertCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out PriceAlertCalculation calculation))
                {
                    await calculation.UnsubscribeAsync(alert);
                }
            };
        }

        public void Subscribe(string symbol)
        {
            if (_streams.ContainsKey(symbol))
            {
                return;
            }

            var stream = _serviceProvider.GetRequiredService<IPriceStream>();

            _ = Task.Run(async () =>
            {
                await stream.InitAsync(symbol);
                await stream.SubscribeAsync((_, price) =>
                {
                    // TODO: Multiple mapping according to route

                    if (_priceChangeCalculations.TryGetValue(symbol, out PriceChangeCalculation calc))
                    {
                        _ = calc.CalculateAsync(price);
                    }
                });
                await stream.SubscribeAsync((_, price) =>
                {
                    // TODO: Multiple mapping according to route

                    if (_priceAlertCalculations.TryGetValue(symbol, out PriceAlertCalculation calc))
                    {
                        _ = calc.CalculateAsync(price);
                    }
                });
                await stream.StartAsync();

                _streams.Add(symbol, stream);
            });
        }

        public void SubscribeAll()
        {
            var uniques = new List<string>();
            var symbols = _symbolStore.Get();

            foreach (var symbol in symbols)
            {
                var route = JsonDocument.Parse(symbol.Route);

                foreach (var item in route.RootElement.EnumerateArray())
                {
                    var baseCurrency = item.GetProperty("Base").GetString().ToUpperInvariant();
                    var quoteCurrency = item.GetProperty("Quote").GetString().ToUpperInvariant();
                    var symbolName = string.Concat(baseCurrency, quoteCurrency);

                    if (!uniques.Contains(symbolName))
                    {
                        uniques.Add(symbolName);
                    }
                }
            }

            uniques.ForEach(Subscribe);
        }

        public void UnsubscribeAll()
        {
            _streams.Values.ToList().ForEach(async stream =>
            {
                await stream.StopAsync();
            });
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
    }
}