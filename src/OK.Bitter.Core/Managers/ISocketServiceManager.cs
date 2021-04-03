using System.Collections.Generic;
using OK.Bitter.Common.Models;

namespace OK.Bitter.Core.Managers
{
    public interface ISocketServiceManager
    {
        void Subscribe(SymbolModel symbol);

        void UpdateUsers();

        void ResetCache(string userId, string symbolId = null);

        void UpdateSubscriptions();

        void UpdateSubscription(string userId);

        void UpdateAlerts();

        void UpdateAlert(string userId);

        List<string> CheckStatus();

        string CheckSymbolStatus(string symbolId);

        void UpdateSymbols();

        void UpdatePrices();

        void SubscribeAll();

        void UnsubscribeAll();
    }
}