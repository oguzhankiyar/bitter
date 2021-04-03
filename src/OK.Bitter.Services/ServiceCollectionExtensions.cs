using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Core.Services;
using OK.Bitter.Services.Call.IFTTT;
using OK.Bitter.Services.Message.Telegram;

namespace OK.Bitter.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTelegramMessageService(this IServiceCollection services, TelegramMessageServiceConfig config)
        {
            services.AddSingleton(config);

            services.AddSingleton<IMessageService, TelegramMessageService>();

            return services;
        }

        public static IServiceCollection AddIFTTTCallService(this IServiceCollection services, IFTTTCallServiceConfig config)
        {
            services.AddSingleton(config);

            services.AddSingleton<ICallService, IFTTTCallService>();

            return services;
        }
    }
}