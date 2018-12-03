using System.Threading.Tasks;

namespace OK.Bitter.Core.Services
{
    public interface IMessageService
    {
        Task<bool> SendMessageAsync(string userId, string message);
    }
}