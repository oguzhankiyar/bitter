using OK.Bitter.Common.Models;
using System;
using System.Collections.Generic;

namespace OK.Bitter.Core.Managers
{
    public interface IAlertManager
    {
        List<AlertModel> GetAlerts();

        List<AlertModel> GetAlertsByUser(string userId);

        bool UpdateAsAlerted(string userId, string symbolId, DateTime date);
    }
}