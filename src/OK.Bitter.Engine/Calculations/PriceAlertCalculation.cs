using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;

namespace OK.Bitter.Engine.Calculations
{
    public class PriceAlertCalculation
    {
        private readonly IUserManager _userManager;
        private readonly IAlertManager _alertManager;

        private SymbolModel _symbol;
        private List<AlertModel> _alerts;
        private IDictionary<string, (decimal Price, bool IsReverse)> _routePrices;

        public PriceAlertCalculation(
            IUserManager userManager,
            IAlertManager alertManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _alertManager = alertManager ?? throw new ArgumentNullException(nameof(alertManager));
        }

        public Task InitAsync(SymbolModel symbol)
        {
            _symbol = symbol;
            _alerts = new List<AlertModel>();
            _routePrices = new Dictionary<string, (decimal Price, bool IsReverse)>();

            var route = JsonDocument.Parse(_symbol.Route);

            foreach (var item in route.RootElement.EnumerateArray())
            {
                var baseCurrency = item.GetProperty("Base").GetString().ToUpperInvariant();
                var quoteCurrency = item.GetProperty("Quote").GetString().ToUpperInvariant();
                var isReverse = item.GetProperty("IsReverse").GetBoolean();

                _routePrices.Add(string.Concat(baseCurrency, quoteCurrency), (0, isReverse));
            }

            return Task.CompletedTask;
        }

        public Task SubscribeAsync(AlertModel alert)
        {
            _alerts.Add(alert);

            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(AlertModel alert)
        {
            var current = _alerts.FirstOrDefault(x => x.UserId == alert.UserId && x.SymbolId == alert.SymbolId);

            if (current != null)
            {
                _alerts.Remove(current);
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

            var alerts = _alerts.Where(x => (x.LessValue.HasValue && x.LessValue.Value >= symbolPrice) || (x.GreaterValue.HasValue && x.GreaterValue <= symbolPrice));

            foreach (var alert in alerts)
            {
                if (alert.LastAlertDate == null || (DateTime.UtcNow - alert.LastAlertDate.Value).TotalMinutes > 5)
                {
                    var message = string.Format("[ALERT] {0}: {1} {2}",
                            _symbol.Base,
                            symbolPrice.ToString("0.########"),
                            _symbol.Quote);

                    _userManager.SendMessage(alert.UserId, message);

                    _userManager.CallUser(alert.UserId, $"{_symbol.FriendlyName} price is {symbolPrice}");

                    alert.LastAlertDate = DateTime.UtcNow;

                    _alertManager.UpdateAsAlerted(alert.UserId, _symbol.Id, DateTime.UtcNow);
                }
            }

            return Task.CompletedTask;
        }
    }
}