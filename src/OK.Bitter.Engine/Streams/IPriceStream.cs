using System;
using System.Threading;
using System.Threading.Tasks;
using OK.Bitter.Common.Models;

namespace OK.Bitter.Engine.Streams
{
    public interface IPriceStream
    {
        string State { get; }

        Task InitAsync(SymbolModel symbol, Action<PriceModel> handler);
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}