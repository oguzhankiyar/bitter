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

            Action reploadSymbolMap = () =>
            {
                _symbolStore.Get()
                    .ForEach(x =>
                    {
                        var symbolName = string.Concat(x.Base, x.Quote);

                        var relateds = GetRelatedSymbols(x);

                        foreach (var related in relateds)
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
                    });
            };

            _symbolStore.OnInserted += (_, symbol) =>
            {
                reploadSymbolMap();
            };

            _symbolStore.OnUpdated += (_, symbol) =>
            {
                reploadSymbolMap();
            };

            _symbolStore.OnDeleted += (_, symbol) =>
            {
                reploadSymbolMap();
            };

            _subscriptionStore.OnInserted += async (_, subscription) =>
            {
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
                var symbol = _symbolStore.Find(x => x.Id == subscription.SymbolId);

                if (_priceChangeCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out PriceChangeCalculation calculation))
                {
                    await calculation.UnsubscribeAsync(subscription);
                }
            };

            _alertStore.OnInserted += async (_, alert) =>
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
            };

            _alertStore.OnUpdated += async (_, alert) =>
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
            };

            _alertStore.OnDeleted += async (_, alert) =>
            {
                var symbol = _symbolStore.Find(x => x.Id == alert.SymbolId);

                if (_priceAlertCalculations.TryGetValue(string.Concat(symbol.Base, symbol.Quote), out PriceAlertCalculation calculation))
                {
                    await calculation.UnsubscribeAsync(alert);
                }
            };
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

        public async Task SubscribeAsync()
        {
            var uniques = new List<string>();
            var symbols = _symbolStore.Get();

            foreach (var symbol in symbols)
            {
                if (!_priceChangeCalculations.ContainsKey(string.Concat(symbol.Base, symbol.Quote)))
                {
                    var calculation = _serviceProvider.GetRequiredService<PriceChangeCalculation>();
                    await calculation.InitAsync(symbol);
                    _priceChangeCalculations.Add(string.Concat(symbol.Base, symbol.Quote), calculation);
                }

                foreach (var item in symbol.Route)
                {
                    var symbolName = string.Concat(item.Base, item.Quote);

                    if (!uniques.Contains(symbolName))
                    {
                        uniques.Add(symbolName);
                    }
                }
            }

            foreach (var unique in uniques)
            {
                if (_streams.ContainsKey(unique))
                {
                    continue;
                }

                var stream = _serviceProvider.GetRequiredService<IPriceStream>();

                await stream.InitAsync(unique);
                await stream.SubscribeAsync((_, price) =>
                {
                    if (_symbolMap.TryGetValue(price.SymbolId, out var items))
                    {
                        foreach (var item in items)
                        {
                            if (_priceChangeCalculations.TryGetValue(item, out PriceChangeCalculation calc))
                            {
                                _ = calc.CalculateAsync(price.SymbolId, price.Date, price.Price);
                            }
                        }
                    }
                });
                await stream.SubscribeAsync((_, price) =>
                {
                    if (_symbolMap.TryGetValue(price.SymbolId, out var items))
                    {
                        foreach (var item in items)
                        {
                            if (_priceAlertCalculations.TryGetValue(item, out PriceAlertCalculation calc))
                            {
                                _ = calc.CalculateAsync(price.SymbolId, price.Date, price.Price);
                            }
                        }
                    }
                });
                await stream.StartAsync();

                _streams.Add(unique, stream);
            }
        }

        public Task UnsubscribeAsync()
        {
            _streams.Values.ToList().ForEach(async stream =>
            {
                await stream.StopAsync();
            });

            return Task.CompletedTask;
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