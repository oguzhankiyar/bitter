using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Calculations;
using OK.Bitter.Engine.Streams;

namespace OK.Bitter.Engine.Managers
{
    public class SocketServiceManager : ISocketServiceManager
    {
        public List<UserModel> Users { get; set; }
        public List<SymbolModel> Symbols { get; set; }
        public List<PriceModel> Prices { get; set; }
        public List<SubscriptionModel> SymbolSubscriptions { get; set; }
        public List<AlertModel> SymbolAlerts { get; set; }
        public IDictionary<string, IPriceStream> SymbolStreams { get; set; }
        public IDictionary<string, PriceChangeCalculation> SymbolPriceChangeCalculations { get; set; }
        public IDictionary<string, PriceAlertCalculation> SymbolPriceAlertCalculations { get; set; }

        private readonly IUserManager _userManager;
        private readonly IAlertManager _alertManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ISymbolManager _symbolManager;
        private readonly IPriceManager _priceManager;
        private readonly IServiceProvider _serviceProvider;

        public SocketServiceManager(
            IUserManager userManager,
            IAlertManager alertManager,
            ISubscriptionManager subscriptionManager,
            ISymbolManager symbolManager,
            IPriceManager priceManager,
            IServiceProvider serviceProvider)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _alertManager = alertManager ?? throw new ArgumentNullException(nameof(alertManager));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _symbolManager = symbolManager ?? throw new ArgumentNullException(nameof(symbolManager));
            _priceManager = priceManager ?? throw new ArgumentNullException(nameof(priceManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            Users = new List<UserModel>();
            Symbols = new List<SymbolModel>();
            Prices = new List<PriceModel>();
            SymbolSubscriptions = new List<SubscriptionModel>();
            SymbolAlerts = new List<AlertModel>();
            SymbolStreams = new Dictionary<string, IPriceStream>();
            SymbolPriceChangeCalculations = new ConcurrentDictionary<string, PriceChangeCalculation>();
            SymbolPriceAlertCalculations = new ConcurrentDictionary<string, PriceAlertCalculation>();
        }

        public void Subscribe(SymbolModel symbol)
        {
            if (SymbolStreams.ContainsKey(symbol.Id))
            {
                return;
            }

            var stream = _serviceProvider.GetRequiredService<IPriceStream>();

            _ = Task.Run(async () =>
            {
                await stream.InitAsync(symbol);
                await stream.SubscribeAsync((_, price) =>
                {
                    if (SymbolPriceChangeCalculations.TryGetValue(symbol.Id, out PriceChangeCalculation calc))
                    {
                        _ = calc.CalculateAsync(price);
                    }
                });
                await stream.SubscribeAsync((_, price) =>
                {
                    if (SymbolPriceAlertCalculations.TryGetValue(symbol.Id, out PriceAlertCalculation calc))
                    {
                        _ = calc.CalculateAsync(price);
                    }
                });
                await stream.StartAsync();

                SymbolStreams.Add(symbol.Id, stream);
            });
        }

        public void SubscribeAll()
        {
            Symbols.ForEach(symbol => Subscribe(symbol));
        }

        public void UnsubscribeAll()
        {
            SymbolStreams.Values.ToList().ForEach(async stream =>
            {
                await stream.StopAsync();
            });
        }

        public void UpdateUsers()
        {
            Users = _userManager.GetUsers();
        }

        public void UpdateSymbols()
        {
            Symbols = _symbolManager.GetSymbols();
        }

        public void UpdatePrices()
        {
            Prices = _priceManager.GetLastPrices();
        }

        public void ResetCache(string userId, string symbolId = null)
        {
            var subscriptions = SymbolSubscriptions.Where(x => x.UserId == userId);

            if (!string.IsNullOrEmpty(symbolId))
            {
                subscriptions = subscriptions.Where(x => x.SymbolId == symbolId).ToList();
            }

            foreach (var subscription in subscriptions)
            {
                subscription.LastNotifiedPrice = decimal.Zero;
                subscription.LastNotifiedDate = DateTime.Now;
            }
        }

        public void UpdateSubscriptions()
        {
            var existings = SymbolSubscriptions;
            foreach (var existing in existings)
            {
                if (SymbolPriceChangeCalculations.TryGetValue(existing.SymbolId, out PriceChangeCalculation calculation))
                {
                    calculation.UnsubscribeAsync(existing);
                }
            }

            SymbolSubscriptions = _subscriptionManager.GetSubscriptions();

            foreach (var subscription in SymbolSubscriptions)
            {
                _ = Task.Run(async () =>
                {
                    var symbol = Symbols.FirstOrDefault(x => x.Id == subscription.SymbolId);

                    if (SymbolPriceChangeCalculations.TryGetValue(symbol.Id, out PriceChangeCalculation existing))
                    {
                        await existing.SubscribeAsync(subscription);
                    }
                    else
                    {
                        var calculation = _serviceProvider.GetRequiredService<PriceChangeCalculation>();
                        await calculation.InitAsync(symbol);
                        await calculation.SubscribeAsync(subscription);
                        SymbolPriceChangeCalculations.Add(symbol.Id, calculation);
                    }
                });
            }
        }

        public void UpdateSubscription(string userId)
        {
            var existings = SymbolSubscriptions.Where(x => x.UserId == userId);
            foreach (var existing in existings)
            {
                if (SymbolPriceChangeCalculations.TryGetValue(existing.SymbolId, out PriceChangeCalculation calculation))
                {
                    calculation.UnsubscribeAsync(existing);
                }
            }

            SymbolSubscriptions.RemoveAll(x => x.UserId == userId);

            var subscriptions = _subscriptionManager.GetSubscriptionsByUser(userId);

            foreach (var subscription in subscriptions)
            {
                SymbolSubscriptions.Add(subscription);

                _ = Task.Run(async () =>
                {
                    var symbol = Symbols.FirstOrDefault(x => x.Id == subscription.SymbolId);

                    if (SymbolPriceChangeCalculations.TryGetValue(symbol.Id, out PriceChangeCalculation existing))
                    {
                        await existing.SubscribeAsync(subscription);
                    }
                    else
                    {
                        var calculation = _serviceProvider.GetRequiredService<PriceChangeCalculation>();
                        await calculation.InitAsync(symbol);
                        await calculation.SubscribeAsync(subscription);
                        SymbolPriceChangeCalculations.Add(symbol.Id, calculation);
                    }
                });
            }
        }

        public void UpdateAlerts()
        {
            var existings = SymbolAlerts;
            foreach (var existing in existings)
            {
                if (SymbolPriceAlertCalculations.TryGetValue(existing.SymbolId, out PriceAlertCalculation calculation))
                {
                    calculation.UnsubscribeAsync(existing);
                }
            }

            SymbolAlerts = _alertManager.GetAlerts();

            foreach (var alert in SymbolAlerts)
            {
                _ = Task.Run(async () =>
                {
                    var symbol = Symbols.FirstOrDefault(x => x.Id == alert.SymbolId);

                    if (SymbolPriceAlertCalculations.TryGetValue(symbol.Id, out PriceAlertCalculation existing))
                    {
                        await existing.SubscribeAsync(alert);
                    }
                    else
                    {
                        var calculation = _serviceProvider.GetRequiredService<PriceAlertCalculation>();
                        await calculation.InitAsync(symbol);
                        await calculation.SubscribeAsync(alert);
                        SymbolPriceAlertCalculations.Add(symbol.Id, calculation);
                    }
                });
            }
        }

        public void UpdateAlert(string userId)
        {
            var existings = SymbolAlerts.Where(x => x.UserId == userId);
            foreach (var existing in existings)
            {
                if (SymbolPriceAlertCalculations.TryGetValue(existing.SymbolId, out PriceAlertCalculation calculation))
                {
                    calculation.UnsubscribeAsync(existing);
                }
            }

            SymbolAlerts.RemoveAll(x => x.UserId == userId);

            var alerts = _alertManager.GetAlertsByUser(userId);

            foreach (var alert in alerts)
            {
                SymbolAlerts.Add(alert);

                _ = Task.Run(async () =>
                {
                    var symbol = Symbols.FirstOrDefault(x => x.Id == alert.SymbolId);

                    if (SymbolPriceAlertCalculations.TryGetValue(symbol.Id, out PriceAlertCalculation existing))
                    {
                        await existing.SubscribeAsync(alert);
                    }
                    else
                    {
                        var calculation = _serviceProvider.GetRequiredService<PriceAlertCalculation>();
                        await calculation.InitAsync(symbol);
                        await calculation.SubscribeAsync(alert);
                        SymbolPriceAlertCalculations.Add(symbol.Id, calculation);
                    }
                });
            }
        }

        public List<string> CheckStatus()
        {
            var symbolSockets = SymbolStreams;

            var lines = new List<string>();

            foreach (var item in symbolSockets)
            {
                var sym = Symbols.FirstOrDefault(x => x.Id == item.Key);

                lines.Add($"{sym.FriendlyName} symbol stream is {item.Value.State}");
            }

            return lines;
        }

        public string CheckSymbolStatus(string symbolId)
        {
            var stream = SymbolStreams.FirstOrDefault(x => x.Key == symbolId);

            var symbol = Symbols.FirstOrDefault(x => x.Id == symbolId);

            return $"{symbol.FriendlyName} symbol stream is {stream.Value.State}";
        }
    }
}