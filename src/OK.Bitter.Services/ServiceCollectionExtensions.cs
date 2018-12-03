using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Core.Services;

namespace OK.Bitter.Services
{
    public static class ServiceCollectionExtensions
    {
        public static void AddServicesLayer(this IServiceCollection services)
        {
            services.AddTransient<ICallService, CallService.CallService>();
            services.AddTransient<IMessageService, TelegramService.TelegramService>();
        }
    }
}