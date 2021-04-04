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
                var symbol = _symbolStore.Find(x => x.Id == subscription.SymbolId);

                if (_priceChangeCalculations.TryGetValue(symbol.Id, out PriceChangeCalculation existing))
                {
                    await existing.SubscribeAsync(subscription);
                }
                else
                {
                    var calculation = _serviceProvider.GetRequiredService<PriceChangeCalculation>();
                    await calculation.InitAsync(symbol);
                    await calculation.SubscribeAsync(subscription);
                    _priceChangeCalculations.Add(symbol.Id, calculation);
                }
            };

            _subscriptionStore.OnUpdated += async (_, subscription) =>
            {
                var symbol = _symbolStore.Find(x => x.Id == subscription.SymbolId);

                if (_priceChangeCalculations.TryGetValue(symbol.Id, out PriceChangeCalculation existing))
                {
                    await existing.SubscribeAsync(subscription);
                }
                else
                {
                    var calculation = _serviceProvider.GetRequiredService<PriceChangeCalculation>();
                    await calculation.InitAsync(symbol);
                    await calculation.SubscribeAsync(subscription);
                    _priceChangeCalculations.Add(symbol.Id, calculation);
                }
            };

            _subscriptionStore.OnDeleted += async (_, subscription) =>
            {
                if (_priceChangeCalculations.TryGetValue(subscription.SymbolId, out PriceChangeCalculation calculation))
                {
                    await calculation.UnsubscribeAsync(subscription);
                }
            };

            _alertStore.OnInserted += async (_, alert) =>
            {
                var symbol = _symbolStore.Find(x => x.Id == alert.SymbolId);

                if (_priceAlertCalculations.TryGetValue(symbol.Id, out PriceAlertCalculation existing))
                {
                    await existing.SubscribeAsync(alert);
                }
                else
                {
                    var calculation = _serviceProvider.GetRequiredService<PriceAlertCalculation>();
                    await calculation.InitAsync(symbol);
                    await calculation.SubscribeAsync(alert);
                    _priceAlertCalculations.Add(symbol.Id, calculation);
                }
            };

            _alertStore.OnUpdated += async (_, alert) =>
            {
                var symbol = _symbolStore.Find(x => x.Id == alert.SymbolId);

                if (_priceAlertCalculations.TryGetValue(symbol.Id, out PriceAlertCalculation existing))
                {
                    await existing.SubscribeAsync(alert);
                }
                else
                {
                    var calculation = _serviceProvider.GetRequiredService<PriceAlertCalculation>();
                    await calculation.InitAsync(symbol);
                    await calculation.SubscribeAsync(alert);
                    _priceAlertCalculations.Add(symbol.Id, calculation);
                }
            };

            _alertStore.OnDeleted += async (_, alert) =>
            {
                if (_priceAlertCalculations.TryGetValue(alert.SymbolId, out PriceAlertCalculation calculation))
                {
                    await calculation.UnsubscribeAsync(alert);
                }
            };
        }

        public void Subscribe(SymbolModel symbol)
        {
            if (_streams.ContainsKey(symbol.Id))
            {
                return;
            }

            var stream = _serviceProvider.GetRequiredService<IPriceStream>();

            _ = Task.Run(async () =>
            {
                await stream.InitAsync(symbol);
                await stream.SubscribeAsync((_, price) =>
                {
                    if (_priceChangeCalculations.TryGetValue(symbol.Id, out PriceChangeCalculation calc))
                    {
                        _ = calc.CalculateAsync(price);
                    }
                });
                await stream.SubscribeAsync((_, price) =>
                {
                    if (_priceAlertCalculations.TryGetValue(symbol.Id, out PriceAlertCalculation calc))
                    {
                        _ = calc.CalculateAsync(price);
                    }
                });
                await stream.StartAsync();

                _streams.Add(symbol.Id, stream);
            });
        }

        public void SubscribeAll()
        {
            _symbolStore.Get().ForEach(symbol => Subscribe(symbol));
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
            var stream = _streams.FirstOrDefault(x => x.Key == symbolId);

            var symbol = _symbolStore.Find(x => x.Id == symbolId);

            return $"{symbol.FriendlyName} symbol stream is {stream.Value.State}";
        }
    }
}