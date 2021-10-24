using System;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("reset")]
    public class ResetCommand : BaseCommand
    {
        private readonly ISymbolRepository _symbolRepository;
        private readonly ISocketManager _socketServiceManager;

        public ResetCommand(
            ISymbolRepository symbolRepository,
            ISocketManager socketManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _symbolRepository = symbolRepository ?? throw new ArgumentNullException(nameof(symbolRepository));
            _socketServiceManager = socketManager ?? throw new ArgumentNullException(nameof(socketManager));
        }

        [CommandCase("{symbol}")]
        public async Task ExecuteAsync(string symbol)
        {
            if (symbol == "all")
            {
                _socketServiceManager.ResetCache(User.Id);

                await ReplyAsync("Success!");
            }
            else
            {
                var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
                if (symbolEntity == null)
                {
                    await ReplyAsync("Symbol is not found!");

                    return;
                }

                _socketServiceManager.ResetCache(User.Id, symbolEntity.Id);

                await ReplyAsync("Success!");
            }
        }
    }
}