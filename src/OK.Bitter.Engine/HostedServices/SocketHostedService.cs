using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Stores;

namespace OK.Bitter.Api.HostedServices
{
    public class SocketHostedService : IHostedService
    {
        private readonly IStore<UserModel> _userStore;
        private readonly IStore<SymbolModel> _symbolStore;
        private readonly IStore<PriceModel> _priceStore;
        private readonly IStore<SubscriptionModel> _subscriptionStore;
        private readonly IStore<AlertModel> _alertStore;
        private readonly ISocketServiceManager _socketServiceManager;

        public SocketHostedService(
            IStore<UserModel> userStore,
            IStore<SymbolModel> symbolStore,
            IStore<PriceModel> priceStore,
            IStore<SubscriptionModel> subscriptionStore,
            IStore<AlertModel> alertStore,
            ISocketServiceManager socketServiceManager)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _symbolStore = symbolStore ?? throw new ArgumentNullException(nameof(symbolStore));
            _priceStore = priceStore ?? throw new ArgumentNullException(nameof(priceStore));
            _subscriptionStore = subscriptionStore ?? throw new ArgumentNullException(nameof(subscriptionStore));
            _alertStore = alertStore ?? throw new ArgumentNullException(nameof(alertStore));
            _socketServiceManager = socketServiceManager ?? throw new ArgumentNullException(nameof(socketServiceManager));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    _userStore.Init();
                    _symbolStore.Init();
                    _priceStore.Init();
                    _subscriptionStore.Init();
                    _alertStore.Init();

                    _socketServiceManager.SubscribeAll();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    _socketServiceManager.UnsubscribeAll();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}