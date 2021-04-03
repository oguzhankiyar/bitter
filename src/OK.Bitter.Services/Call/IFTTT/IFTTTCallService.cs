using OK.Bitter.Core.Services;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OK.Bitter.Services.Call.IFTTT
{
    public class IFTTTCallService : ICallService
    {
        private readonly IFTTTCallServiceConfig _config;

        public IFTTTCallService(IFTTTCallServiceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<bool> CallAsync(string token, string message)
        {
            var json = "{ \"value1\": \"" + message + "\" }";
            var url = string.Concat(_config.Url, token);

            await new HttpClient().PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

            return true;
        }
    }
}