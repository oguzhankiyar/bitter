using System;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("/start")]
    public class StartCommand : BaseCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageService _messageService;

        public StartCommand(
            IUserRepository userRepository,
            IMessageService messageService,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        [CommandCase]
        public async Task StartAsync()
        {
            string result = $"Wellcome @{Context.Username}!\r\n";

            result += "If you want to authenticate this bot, please type /auth command with password.\r\n\r\n";

            result += "Example:\r\n/auth 123456";

            await ReplyAsync(result);

            var admins = _userRepository.FindUsers().Where(x => x.Type == UserTypeEnum.Admin);

            foreach (var admin in admins)
            {
                await _messageService.SendMessageAsync(admin.ChatId, $"@{Context.Username} is joined!");
            }
        }
    }
}