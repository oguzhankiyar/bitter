using Microsoft.Extensions.Configuration;
using OK.Bitter.Core.Services;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OK.Bitter.Services.TelegramService
{
    public class TelegramService : IMessageService
    {
        private readonly string _botToken;

        public TelegramService(IConfiguration configuration)
        {
            _botToken = configuration.GetSection("ServiceConfigurations").GetSection("TelegramService")["BotToken"];
        }

        public async Task<bool> SendMessageAsync(string userId, string message)
        {
            string json = "{\"chat_id\":\"" + userId + "\", \"text\":\"" + message + "\"}";

            string url = "https://api.telegram.org/bot" + _botToken + "/sendMessage";

            await new HttpClient().PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

            return true;
        }
    }
}