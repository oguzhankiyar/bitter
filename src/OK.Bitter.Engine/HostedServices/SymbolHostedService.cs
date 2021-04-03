using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OK.Bitter.Core.Managers;

namespace OK.Bitter.Api.HostedServices
{
    public class SymbolHostedService : IHostedService, IDisposable
    {
        private readonly ISocketServiceManager _socketServiceManager;
        private Timer _timer;

        public SymbolHostedService(ISocketServiceManager socketServiceManager)
        {
            _socketServiceManager = socketServiceManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _socketServiceManager.UpdateSymbols();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}