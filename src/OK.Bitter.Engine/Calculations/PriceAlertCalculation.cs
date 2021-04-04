using System;
using System.Collections.Generic;
using System.Linq;
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

        public Task CalculateAsync(PriceModel price)
        {
            var alerts = _alerts.Where(x => (x.LessValue.HasValue && x.LessValue.Value >= price.Price) || (x.GreaterValue.HasValue && x.GreaterValue <= price.Price));

            foreach (var alert in alerts)
            {
                if (alert.LastAlertDate == null || (DateTime.UtcNow - alert.LastAlertDate.Value).TotalMinutes > 5)
                {
                    var message = $"[ALERT] {_symbol.FriendlyName}: {price.Price}";

                    _userManager.SendMessage(alert.UserId, message);

                    _userManager.CallUser(alert.UserId, $"{_symbol.FriendlyName} price is {price.Price}");

                    alert.LastAlertDate = DateTime.UtcNow;

                    _alertManager.UpdateAsAlerted(alert.UserId, _symbol.Id, DateTime.UtcNow);
                }
            }

            return Task.CompletedTask;
        }
    }
}