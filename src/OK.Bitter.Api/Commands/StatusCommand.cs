using System;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("status")]
    public class StatusCommand : BaseCommand
    {
        private readonly ISymbolRepository _symbolRepository;
        private readonly IMessageService _messageService;
        private readonly ISocketManager _socketManager;

        public StatusCommand(
            ISymbolRepository symbolRepository,
            IMessageService messageService,
            ISocketManager socketManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _symbolRepository = symbolRepository ?? throw new ArgumentNullException(nameof(symbolRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _socketManager = socketManager ?? throw new ArgumentNullException(nameof(socketManager));
        }

        [CommandCase("get", "{symbol}")]
        public async Task GetAsync(string symbol)
        {
            if (User.Type != UserTypeEnum.Admin)
            {
                await ReplyAsync("Unauthorized!");

                return;
            }

            if (symbol == "all")
            {
                var lines = _socketManager.CheckStatus();

                await ReplyPaginatedAsync(lines);

                return;
            }

            var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
            if (symbolEntity == null)
            {
                await ReplyAsync("Symbol is not found!");

                return;
            }

            var symbolStatus = _socketManager.CheckSymbolStatus(symbolEntity.Id);

            await ReplyAsync(symbolStatus);
        }
    }
}