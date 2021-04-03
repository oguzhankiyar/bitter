using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Api.HostedServices;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Managers;
using OK.Bitter.Engine.Streams;

namespace OK.Bitter.Engine
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEngineLayer(this IServiceCollection services)
        {
            services.AddTransient<IAlertManager, AlertManager>();
            services.AddTransient<IPriceManager, PriceManager>();
            services.AddTransient<ISubscriptionManager, SubscriptionManager>();
            services.AddTransient<ISymbolManager, SymbolManager>();
            services.AddTransient<IUserManager, UserManager>();
            services.AddSingleton<ISocketServiceManager, SocketServiceManager>();

            services.AddTransient<IPriceStream, PriceStream>();

            services.AddHostedService<SocketHostedService>();
            services.AddHostedService<SymbolHostedService>();

            return services;
        }
    }
}