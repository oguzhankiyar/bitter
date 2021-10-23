﻿using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Calculations;
using OK.Bitter.Engine.HostedServices;
using OK.Bitter.Engine.Managers;
using OK.Bitter.Engine.Stores;
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
            services.AddSingleton<ISocketManager, SocketManager>();

            services.AddSingleton<IStore<UserModel>, UserStore>();
            services.AddSingleton<IStore<SymbolModel>, SymbolStore>();
            services.AddSingleton<IStore<PriceModel>, PriceStore>();
            services.AddSingleton<IStore<SubscriptionModel>, SubscriptionStore>();
            services.AddSingleton<IStore<AlertModel>, AlertStore>();

            services.AddTransient<IPriceStream, PriceStream>();

            services.AddTransient<PriceChangeCalculation>();
            services.AddTransient<PriceAlertCalculation>();
            services.AddTransient<UserBalanceCalculation>();
            services.AddTransient<UserPnLCalculation>();

            services.AddHostedService<SocketHostedService>();
            services.AddHostedService<SymbolHostedService>();
            services.AddHostedService<PriceHostedService>();

            return services;
        }
    }
}