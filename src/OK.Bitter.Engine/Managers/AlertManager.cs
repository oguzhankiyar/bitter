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
            return _alertRepository
                .GetList()
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
            return _alertRepository
                .GetList(x => x.UserId == userId)
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
            var alert = _alertRepository.Get(x => x.UserId == userId && x.SymbolId == symbolId);

            if (alert == null)
            {
                return false;
            }

            alert.LastAlertDate = date;

            _alertRepository.Save(alert);

            return true;
        }
    }
}