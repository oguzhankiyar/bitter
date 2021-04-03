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
        private readonly ISocketServiceManager _socketServiceManager;

        public StatusCommand(
            ISymbolRepository symbolRepository,
            IMessageService messageService,
            ISocketServiceManager socketServiceManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _symbolRepository = symbolRepository ?? throw new ArgumentNullException(nameof(symbolRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _socketServiceManager = socketServiceManager ?? throw new ArgumentNullException(nameof(socketServiceManager));
        }

        [CommandCase("get", "{symbol}")]
        public async Task GetAsync(string symbol)
        {
            if (User == null || User.Type != UserTypeEnum.Admin)
            {
                await ReplyAsync("Unauthorized!");

                return;
            }

            if (symbol == "all")
            {
                var allStatus = _socketServiceManager.CheckStatus();

                await ReplyAsync(allStatus);

                return;
            }

            var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
            if (symbolEntity == null)
            {
                await ReplyAsync("Symbol is not found!");

                return;
            }

            var symbolStatus = _socketServiceManager.CheckSymbolStatus(symbolEntity.Id);

            await ReplyAsync(symbolStatus);
        }
    }
}