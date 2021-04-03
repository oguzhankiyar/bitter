using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OK.Bitter.Core.Managers;

namespace OK.Bitter.Api.HostedServices
{
    public class SocketHostedService : IHostedService
    {
        private readonly ISocketServiceManager _socketServiceManager;

        public SocketHostedService(ISocketServiceManager socketServiceManager)
        {
            _socketServiceManager = socketServiceManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    _socketServiceManager.UpdateSymbols();
                    _socketServiceManager.UpdatePrices();
                    _socketServiceManager.UpdateUsers();
                    _socketServiceManager.UpdateSubscriptions();
                    _socketServiceManager.UpdateAlerts();

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