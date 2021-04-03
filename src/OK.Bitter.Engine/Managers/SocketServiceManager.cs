using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Extensions;
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

        private IDictionary<string, object> symbolNotifyLockObjects = new Dictionary<string, object>();
        private IDictionary<string, object> symbolAlertLockObjects = new Dictionary<string, object>();
        private IDictionary<string, object> symbolPriceLockObjects = new Dictionary<string, object>();

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
        }

        public void Subscribe(SymbolModel symbol)
        {
            if (SymbolStreams.ContainsKey(symbol.Id))
            {
                return;
            }

            symbolNotifyLockObjects.Add(symbol.Id, new { });
            symbolAlertLockObjects.Add(symbol.Id, new { });
            symbolPriceLockObjects.Add(symbol.Id, new { });

            var stream = _serviceProvider.GetRequiredService<IPriceStream>();

            _ = Task.Run(async () =>
            {
                await stream.InitAsync(symbol, message => OnPriceChanged(symbol, message));

                await stream.StartAsync();

                SymbolStreams.Add(symbol.Id, stream);
            });
        }

        private void OnPriceChanged(SymbolModel symbol, PriceModel price)
        {
            AlertPriceAsync(symbol, price.Price);

            var symbolPrice = Prices.FirstOrDefault(x => x.SymbolId == symbol.Id);

            if (symbolPrice.Price == decimal.Zero)
            {
                symbolPrice.Date = price.Date;
                symbolPrice.Price = price.Price;
            }
            else
            {
                var change = (price.Price - symbolPrice.Price) / symbolPrice.Price;

                if (Math.Abs(change) >= symbol.MinimumChange)
                {
                    lock (symbolNotifyLockObjects[symbol.Id])
                    {
                        NotifyPriceAsync(symbol, price.Price);

                        _priceManager.SaveLastPrice(symbol.Id, price.Price, change, symbolPrice.Date, DateTime.Now);

                        symbolPrice.Price = price.Price;
                        symbolPrice.Date = price.Date;
                    }
                }
            }
        }

        private void AlertPriceAsync(SymbolModel symbol, decimal currentPrice)
        {
            Task.Run(() =>
            {
                var symbolAlerts = SymbolAlerts.Where(x => x.SymbolId == symbol.Id && ((x.LessValue.HasValue && x.LessValue.Value >= currentPrice) || (x.GreaterValue.HasValue && x.GreaterValue <= currentPrice)));

                foreach (var symbolAlert in symbolAlerts)
                {
                    if (symbolAlert.LastAlertDate == null || (DateTime.Now - symbolAlert.LastAlertDate.Value).TotalMinutes > 5)
                    {
                        var user = Users.FirstOrDefault(x => x.Id == symbolAlert.UserId);

                        string message = $"[ALERT] {symbol.FriendlyName}: {currentPrice}";

                        _userManager.SendMessage(user.Id, message);

                        _userManager.CallUser(symbolAlert.UserId, $"{symbol.FriendlyName} price is {currentPrice}");

                        symbolAlert.LastAlertDate = DateTime.Now;

                        _alertManager.UpdateAsAlerted(user.Id, symbol.Id, DateTime.Now);
                    }
                }
            });
        }

        private void NotifyPriceAsync(SymbolModel symbol, decimal currentPrice)
        {
            var symbolSubscriptions = SymbolSubscriptions.Where(x => x.SymbolId == symbol.Id);

            foreach (var symbolSubscription in symbolSubscriptions)
            {
                if (symbolSubscription.LastNotifiedPrice == decimal.Zero)
                {
                    symbolSubscription.LastNotifiedPrice = currentPrice;
                    symbolSubscription.LastNotifiedDate = DateTime.Now;
                }
                else
                {
                    decimal userPrice = symbolSubscription.LastNotifiedPrice;
                    decimal userChange = (currentPrice - userPrice) / userPrice;

                    if (Math.Abs(userChange) >= symbolSubscription.MinimumChange)
                    {
                        var user = Users.FirstOrDefault(x => x.Id == symbolSubscription.UserId);

                        string message = string.Format("{0}: {1} [{2}% {3}]",
                            symbol.FriendlyName,
                            currentPrice,
                            (userChange * 100).ToString("+0.00;-0.00;0"),
                            (DateTime.Now - symbolSubscription.LastNotifiedDate).ToIntervalString());

                        _userManager.SendMessage(user.Id, message);

                        // Console.WriteLine("Notified {0}: {1}", user.Id, message);

                        symbolSubscription.LastNotifiedPrice = currentPrice;
                        symbolSubscription.LastNotifiedDate = DateTime.Now;

                        _subscriptionManager.UpdateAsNotified(user.Id, symbol.Id, currentPrice, DateTime.Now);
                    }
                }
            }
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
            SymbolSubscriptions = _subscriptionManager.GetSubscriptions();
        }

        public void UpdateSubscription(string userId)
        {
            SymbolSubscriptions.RemoveAll(x => x.UserId == userId);

            var subscriptions = _subscriptionManager.GetSubscriptionsByUser(userId);

            foreach (var subscription in subscriptions)
            {
                SymbolSubscriptions.Add(subscription);
            }
        }

        public void UpdateAlerts()
        {
            SymbolAlerts = _alertManager.GetAlerts();
        }

        public void UpdateAlert(string userId)
        {
            SymbolAlerts.RemoveAll(x => x.UserId == userId);

            var alerts = _alertManager.GetAlertsByUser(userId);

            foreach (var alert in alerts)
            {
                SymbolAlerts.Add(alert);
            }
        }

        public string CheckStatus()
        {
            var symbolSockets = SymbolStreams;

            string message = string.Empty;

            foreach (var item in symbolSockets)
            {
                var sym = Symbols.FirstOrDefault(x => x.Id == item.Key);

                message += $"{sym.FriendlyName} symbol stream is {item.Value.State}\r\n";
            }

            return message;
        }

        public string CheckSymbolStatus(string symbolId)
        {
            var stream = SymbolStreams.FirstOrDefault(x => x.Key == symbolId);

            var symbol = Symbols.FirstOrDefault(x => x.Id == symbolId);

            return $"{symbol.FriendlyName} symbol stream is {stream.Value.State}";
        }
    }
}