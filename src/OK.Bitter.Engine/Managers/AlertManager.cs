using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OK.Bitter.Engine.Managers
{
    public class AlertManager : IAlertManager
    {
        private readonly IAlertRepository _alertRepository;

        public AlertManager(IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public List<AlertModel> GetAlerts()
        {
            return _alertRepository.FindAlerts()
                                   .Select(x => new AlertModel()
                                   {
                                       UserId = x.UserId,
                                       SymbolId = x.SymbolId,
                                       LessValue = x.LessValue,
                                       GreaterValue = x.GreaterValue
                                   })
                                   .ToList();
        }

        public List<AlertModel> GetAlertsByUser(string userId)
        {
            return _alertRepository.FindAlerts(userId)
                                   .Select(x => new AlertModel()
                                   {
                                       UserId = x.UserId,
                                       SymbolId = x.SymbolId,
                                       LessValue = x.LessValue,
                                       GreaterValue = x.GreaterValue
                                   })
                                   .ToList();
        }

        public bool UpdateAsAlerted(string userId, string symbolId, DateTime date)
        {
            var alert = _alertRepository.FindAlert(userId, symbolId);

            if (alert == null)
            {
                return false;
            }

            alert.LastAlertDate = date;

            return _alertRepository.UpdateAlert(alert);
        }
    }
}