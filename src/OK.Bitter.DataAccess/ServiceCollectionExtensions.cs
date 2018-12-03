using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;
using OK.Bitter.DataAccess.Repositories;

namespace OK.Bitter.DataAccess
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDataAccessLayer(this IServiceCollection services, string connectionString)
        {
            services.AddTransient((sp) =>
            {
                return new BitterDataContext(connectionString, "Bitter");
            });

            services.AddTransient<IAlertRepository, AlertRepository>();
            services.AddTransient<IMessageRepository, MessageRepository>();
            services.AddTransient<IPriceRepository, PriceRepository>();
            services.AddTransient<ISettingRepository, SettingRepository>();
            services.AddTransient<ISubscriptionRepository, SubscriptionRepository>();
            services.AddTransient<ISymbolRepository, SymbolRepository>();
            services.AddTransient<IUserRepository, UserRepository>();
        }
    }
}