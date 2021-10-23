using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Stores;

namespace OK.Bitter.Engine.HostedServices
{
    public class SocketHostedService : IHostedService
    {
        private readonly IStore<UserModel> _userStore;
        private readonly IStore<SymbolModel> _symbolStore;
        private readonly IStore<PriceModel> _priceStore;
        private readonly IStore<SubscriptionModel> _subscriptionStore;
        private readonly IStore<AlertModel> _alertStore;
        private readonly ISocketManager _socketManager;

        public SocketHostedService(
            IStore<UserModel> userStore,
            IStore<SymbolModel> symbolStore,
            IStore<PriceModel> priceStore,
            IStore<SubscriptionModel> subscriptionStore,
            IStore<AlertModel> alertStore,
            ISocketManager socketManager)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _symbolStore = symbolStore ?? throw new ArgumentNullException(nameof(symbolStore));
            _priceStore = priceStore ?? throw new ArgumentNullException(nameof(priceStore));
            _subscriptionStore = subscriptionStore ?? throw new ArgumentNullException(nameof(subscriptionStore));
            _alertStore = alertStore ?? throw new ArgumentNullException(nameof(alertStore));
            _socketManager = socketManager ?? throw new ArgumentNullException(nameof(socketManager));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _userStore.Init();
            _symbolStore.Init();
            _priceStore.Init();
            _subscriptionStore.Init();
            _alertStore.Init();

            await _socketManager.SubscribeAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _socketManager.UnsubscribeAsync();
        }
    }
}