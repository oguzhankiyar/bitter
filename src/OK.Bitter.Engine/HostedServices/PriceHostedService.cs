using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OK.Bitter.Core.Managers;

namespace OK.Bitter.Api.HostedServices
{
    public class PriceHostedService : IHostedService, IDisposable
    {
        private readonly IPriceManager _priceManager;

        private Timer _timer;

        public PriceHostedService(IPriceManager priceManager)
        {
            _priceManager = priceManager ?? throw new ArgumentNullException(nameof(priceManager));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(6));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var startDate = DateTime.UtcNow.AddDays(-7);

            _priceManager.RemoveOldPrices(startDate);
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