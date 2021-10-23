using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Stores;

namespace OK.Bitter.Engine.HostedServices
{
    public class SymbolHostedService : IHostedService, IDisposable
    {
        private readonly ISymbolManager _symbolManager;
        private readonly IStore<SymbolModel> _symbolStore;

        private Timer _timer;

        public SymbolHostedService(
            ISymbolManager symbolManager,
            IStore<SymbolModel> symbolStore)
        {
            _symbolManager = symbolManager ?? throw new ArgumentNullException(nameof(symbolManager));
            _symbolStore = symbolStore ?? throw new ArgumentNullException(nameof(symbolStore));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _symbolManager
                .GetSymbols()
                .ForEach(_symbolStore.Upsert);
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