using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System.Linq;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public class StartCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageService _messageService;

        public StartCommand(IUserRepository userRepository,
                            IMessageService messageService)
        {
            _userRepository = userRepository;
            _messageService = messageService;
        }

        public async Task ExecuteAsync(BotUpdateInput input)
        {
            string result = $"Wellcome @{input.Message.From.Username}!\r\n";

            result += "If you want to authenticate this bot, please type /auth command with password.\r\n\r\n";

            result += "Example:\r\n/auth 123456";

            await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), result);

            var admins = _userRepository.FindUsers().Where(x => x.Type == UserTypeEnum.Admin);

            foreach (var admin in admins)
            {
                await _messageService.SendMessageAsync(admin.ChatId, $"@{input.Message.From.Username} is joined!");
            }
        }
    }
}