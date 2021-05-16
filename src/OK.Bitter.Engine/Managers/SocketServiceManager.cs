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

        private readonly IDictionary<string, List<string>> _symbolMap;
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

                        var relateds = GetRelatedSymbols(x.Route);

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

        private List<string> GetRelatedSymbols(string route)
        {
            var symbols = new List<string>();

            var json = JsonDocument.Parse(route);

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var baseCurrency = item.GetProperty("Base").GetString().ToUpperInvariant();
                var quoteCurrency = item.GetProperty("Quote").GetString().ToUpperInvariant();

                var symbolName = string.Concat(baseCurrency, quoteCurrency);

                if (!symbols.Contains(symbolName))
                {
                    symbols.Add(symbolName);
                }
            }

            return symbols;
        }

        public void Subscribe(List<string> symbols)
        {
            if (_streams.ContainsKey("ALL"))
            {
                return;
            }

            var stream = _serviceProvider.GetRequiredService<IPriceStream>();

            _ = Task.Run(async () =>
            {
                await stream.InitAsync(symbols);
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

                _streams.Add("ALL", stream);
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

            Subscribe(uniques);
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