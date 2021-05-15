using System;
using System.Collections.Generic;
using System.Linq;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;

namespace OK.Bitter.Engine.Stores
{
    public class AlertStore : IStore<AlertModel>
    {
        public event EventHandler<AlertModel> OnInserted;
        public event EventHandler<AlertModel> OnUpdated;
        public event EventHandler<AlertModel> OnDeleted;

        private readonly IAlertManager _alertManager;

        private List<AlertModel> _items = new List<AlertModel>();

        public AlertStore(IAlertManager alertManager)
        {
            _alertManager = alertManager ?? throw new ArgumentNullException(nameof(alertManager));
        }

        public void Init()
        {
            _alertManager
                .GetAlerts()
                .ForEach(Upsert);
        }

        public List<AlertModel> Get(Func<AlertModel, bool> expression = null)
        {
            if (expression == null)
            {
                return _items;
            }

            return _items.Where(expression).ToList();
        }

        public AlertModel Find(Func<AlertModel, bool> expression)
        {
            return _items.FirstOrDefault(expression);
        }

        public void Upsert(AlertModel alert)
        {
            var item = _items.FirstOrDefault(x => x.UserId == alert.UserId && x.SymbolId == alert.SymbolId);
            if (item != null)
            {
                item.LessValue = alert.LessValue;
                item.GreaterValue = alert.GreaterValue;
                item.LastAlertDate = alert.LastAlertDate;
                OnUpdated?.Invoke(this, alert);
            }
            else
            {
                _items.Add(alert);
                OnInserted?.Invoke(this, alert);
            }
        }

        public void Delete(AlertModel alert)
        {
            _items.RemoveAll(x => x.UserId == alert.UserId && x.SymbolId == alert.SymbolId);
            OnDeleted?.Invoke(this, alert);
        }

        public void Delete(Func<AlertModel, bool> filter = null)
        {
            var alerts = Get(filter);

            foreach (var alert in alerts)
            {
                _items.RemoveAll(x => x.UserId == alert.UserId && x.SymbolId == alert.SymbolId);
                OnDeleted?.Invoke(this, alert);
            }
        }
    }
}