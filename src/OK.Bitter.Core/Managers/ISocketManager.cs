using System.Collections.Generic;
using System.Threading.Tasks;

namespace OK.Bitter.Core.Managers
{
    public interface ISocketManager
    {
        Task SubscribeAsync();
        Task UnsubscribeAsync();
        void ResetCache(string userId, string symbolId = null);
        List<string> CheckStatus();
        string CheckSymbolStatus(string symbolId);
    }
}