using OK.Bitter.Core.Managers;
using System;
using System.Threading.Tasks;

namespace OK.Bitter.Api.HostedServices
{
    public interface ISocketHostedService
    {
        Task StartAsync();

        Task StopAsync();
    }

    public class SocketHostedService : ISocketHostedService
    {
        private readonly ISocketServiceManager _socketServiceManager;

        public SocketHostedService(ISocketServiceManager socketServiceManager)
        {
            _socketServiceManager = socketServiceManager;
        }

        public async Task StartAsync()
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

        public async Task StopAsync()
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