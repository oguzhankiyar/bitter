using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace OK.Bitter.Api.HostedServices
{
    public class ConsumeSocketHostedService : IHostedService
    {
        private readonly ISocketHostedService _socketHostedService;

        public ConsumeSocketHostedService(ISocketHostedService socketHostedService)
        {
            _socketHostedService = socketHostedService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _socketHostedService.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _socketHostedService.StopAsync();
        }
    }
}