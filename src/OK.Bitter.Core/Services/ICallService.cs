using System.Threading.Tasks;

namespace OK.Bitter.Core.Services
{
    public interface ICallService
    {
        Task<bool> CallAsync(string token, string message);
    }
}