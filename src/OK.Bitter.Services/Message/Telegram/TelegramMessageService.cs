using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OK.Bitter.Core.Services;

namespace OK.Bitter.Services.Message.Telegram
{
    public class TelegramMessageService : IMessageService
    {
        private readonly TelegramMessageServiceConfig _config;

        public TelegramMessageService(TelegramMessageServiceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<bool> SendMessageAsync(string userId, string message)
        {
            var json = "{\"chat_id\":\"" + userId + "\", \"text\":\"" + message + "\"}";
            var url = _config.Url + _config.BotToken + "/sendMessage";

            await new HttpClient().PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

            return true;
        }
    }
}