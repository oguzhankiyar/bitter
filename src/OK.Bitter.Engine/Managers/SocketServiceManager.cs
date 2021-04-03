using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSocket4Net;

namespace OK.Bitter.Engine.Managers
{
    public class SocketServiceManager : ISocketServiceManager
    {
        public List<UserModel> Users { get; set; }

        public List<SymbolModel> Symbols { get; set; }

        public List<PriceModel> Prices { get; set; }

        public List<SubscriptionModel> SymbolSubscriptions { get; set; }

        public List<AlertModel> SymbolAlerts { get; set; }

        public IDictionary<string, WebSocket> SymbolSockets { get; set; }

        private IDictionary<string, object> symbolNotifyLockObjects = new Dictionary<string, object>();
        private IDictionary<string, object> symbolAlertLockObjects = new Dictionary<string, object>();
        private IDictionary<string, object> symbolPriceLockObjects = new Dictionary<string, object>();

        private readonly IUserManager _userManager;
        private readonly IAlertManager _alertManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ISymbolManager _symbolManager;
        private readonly IPriceManager _priceManager;

        public SocketServiceManager(IUserManager userManager,
                                   IAlertManager alertManager,
                                   ISubscriptionManager subscriptionManager,
                                   ISymbolManager symbolManager,
                                   IPriceManager priceManager)
        {
            _userManager = userManager;
            _alertManager = alertManager;
            _subscriptionManager = subscriptionManager;
            _symbolManager = symbolManager;
            _priceManager = priceManager;

            Users = new List<UserModel>();
            Symbols = new List<SymbolModel>();
            Prices = new List<PriceModel>();
            SymbolSubscriptions = new List<SubscriptionModel>();
            SymbolAlerts = new List<AlertModel>();
            SymbolSockets = new Dictionary<string, WebSocket>();
        }

        public void Subscribe(SymbolModel symbol)
        {
            if (SymbolSockets.ContainsKey(symbol.Id))
            {
                return;
            }

            symbolNotifyLockObjects.Add(symbol.Id, new { });
            symbolAlertLockObjects.Add(symbol.Id, new { });
            symbolPriceLockObjects.Add(symbol.Id, new { });

            string url = "wss://stream.binance.com:9443/ws/" + symbol.Name.Replace("|", string.Empty).ToLower() + "@aggTrade";

            WebSocket webSocket = new WebSocket(url);

            webSocket.Opened += (s, e) =>
            {
                try
                {
                    Console.WriteLine(symbol.FriendlyName + " stream is open!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(ex));
                }
            };

            webSocket.Closed += (s, e) =>
            {
                Console.WriteLine(symbol.FriendlyName + " stream is closed!");

                try
                {
                    webSocket.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(ex));
                }
            };

            webSocket.MessageReceived += (s, e) =>
            {
                try
                {
                    OnMessageReceived(symbol, e.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(ex));
                }
            };

            webSocket.Open();

            SymbolSockets.Add(symbol.Id, webSocket);
        }


        private void OnMessageReceived(SymbolModel symbol, string message)
        {
            var json = JObject.Parse(message);

            decimal currentPrice = Convert.ToDecimal(json["p"].ToString());

            AlertPriceAsync(symbol, currentPrice);

            var symbolPrice = Prices.FirstOrDefault(x => x.SymbolId == symbol.Id);

            decimal lastPrice = symbolPrice.Price;

            if (lastPrice == decimal.Zero)
            {
                symbolPrice.Date = DateTime.Now;
                symbolPrice.Price = currentPrice;
            }
            else
            {
                decimal change = (currentPrice - lastPrice) / lastPrice;

                if (Math.Abs(change) >= symbol.MinimumChange)
                {
                    lock (symbolNotifyLockObjects[symbol.Id])
                    {
                        NotifyPriceAsync(symbol, currentPrice);

                        _priceManager.SaveLastPrice(symbol.Id, currentPrice, change, symbolPrice.Date, DateTime.Now);

                        symbolPrice.Price = currentPrice;
                        symbolPrice.Date = DateTime.Now;
                    }
                }

                Console.WriteLine($"{symbol.Name} - Price: {currentPrice} Change: {change}");
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
            SymbolSockets.Values.ToList().ForEach(socket =>
            {
                socket.Close();
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
            var symbolSockets = SymbolSockets;

            string message = string.Empty;

            foreach (var item in symbolSockets)
            {
                var sym = Symbols.FirstOrDefault(x => x.Id == item.Key);

                message += $"{sym.FriendlyName} symbol stream is {item.Value.State.ToString()}\r\n";
            }

            return message;
        }

        public string CheckSymbolStatus(string symbolId)
        {
            var symbolSocket = SymbolSockets.FirstOrDefault(x => x.Key == symbolId);

            var sym3 = Symbols.FirstOrDefault(x => x.Id == symbolId);

            return $"{sym3.FriendlyName} symbol stream is {symbolSocket.Value.State.ToString()}";
        }
    }
}