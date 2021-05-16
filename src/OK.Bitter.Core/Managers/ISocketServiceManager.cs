using System.Collections.Generic;
using OK.Bitter.Common.Models;

namespace OK.Bitter.Core.Managers
{
    public interface ISocketServiceManager
    {
        void SubscribeAll();
        void UnsubscribeAll();
        void Subscribe(string symbol);
        void ResetCache(string userId, string symbolId = null);
        List<string> CheckStatus();
        string CheckSymbolStatus(string symbolId);
    }
}