using OK.Bitter.Common.Entities;
using System.Collections.Generic;

namespace OK.Bitter.Core.Repositories
{
    public interface IAlertRepository
    {
        IEnumerable<AlertEntity> FindAlerts();

        IEnumerable<AlertEntity> FindAlerts(string userId);

        AlertEntity FindAlert(string userId, string symbolId);

        AlertEntity InsertAlert(AlertEntity alert);

        bool UpdateAlert(AlertEntity alert);

        bool RemoveAlert(string alertId);
    }
}