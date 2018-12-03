using OK.Bitter.Core.Services;
using System.Net.Http;
using System.Text;

namespace OK.Bitter.Services.CallService
{
    public class CallService : ICallService
    {
        public bool Call(string token, string message)
        {
            string url = "https://maker.ifttt.com/trigger/call/with/key/" + token;
            
            new HttpClient().PostAsync(url, new StringContent("{ \"value1\": \"" + message + "\" }", Encoding.UTF8, "application/json"));

            return true;
        }
    }
}