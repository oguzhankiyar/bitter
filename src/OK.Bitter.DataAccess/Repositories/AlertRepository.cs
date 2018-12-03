using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;
using System.Collections.Generic;

namespace OK.Bitter.DataAccess.Repositories
{
    public class AlertRepository : BaseRepository<AlertEntity>, IAlertRepository
    {
        public AlertRepository(BitterDataContext context) : base(context, "Alerts")
        {
        }

        public IEnumerable<AlertEntity> FindAlerts()
        {
            return GetList();
        }

        public IEnumerable<AlertEntity> FindAlerts(string userId)
        {
            return GetList(x => x.UserId == userId);
        }

        public AlertEntity FindAlert(string userId, string symbolId)
        {
            return Get(x => x.UserId == userId && x.SymbolId == symbolId);
        }

        public AlertEntity InsertAlert(AlertEntity alert)
        {
            return Save(alert);
        }

        public bool UpdateAlert(AlertEntity alert)
        {
            Save(alert);

            return true;
        }

        public bool RemoveAlert(string alertId)
        {
            Delete(alertId);

            return true;
        }
    }
}