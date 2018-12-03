using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public class HelpCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageService _messageService;

        public HelpCommand(IUserRepository userRepository,
                           IMessageService messageService)
        {
            _userRepository = userRepository;
            _messageService = messageService;
        }

        public async Task ExecuteAsync(BotUpdateInput input)
        {
            UserEntity user = _userRepository.FindUser(input.Message.Chat.Id.ToString());

            List<string> lines = new List<string>();

            if (user == null)
            {
                lines.Add("Authenticate the bot");
                lines.Add("/auth <password>");
                lines.Add(string.Empty);
            }
            else
            {
                if (user.Type == UserTypeEnum.Admin)
                {
                    lines.Add("Send command to console");
                    lines.Add("/console <command>");
                    lines.Add(string.Empty);

                    lines.Add("Send message to all users");
                    lines.Add("/message <message>");
                    lines.Add(string.Empty);

                    lines.Add("Get symbol streams status");
                    lines.Add("/status get <all|symbol>");
                    lines.Add(string.Empty);
                }

                lines.Add("Manage your subscriptions");
                lines.Add("/subscriptions get <all|symbol>");
                lines.Add("/subscriptions set <all|symbol>");
                lines.Add("/subscriptions del <all|symbol>");
                lines.Add(string.Empty);

                lines.Add("Manage your alerts");
                lines.Add("/alerts get <all|symbol>");
                lines.Add("/alerts set <symbol> less <value>");
                lines.Add("/alerts set <symbol> greater <value>");
                lines.Add("/alerts del <symbol>");
                lines.Add(string.Empty);

                lines.Add("Get price history");
                lines.Add("/prices get <all|symbol> <interval>");
                lines.Add(string.Empty);

                lines.Add("Manage your settings");
                lines.Add("/settings get <all|key>");
                lines.Add("/settings set <key> <value>");
                lines.Add("/settings del <key>");
                lines.Add(string.Empty);

                lines.Add("Reset a symbol changes cache");
                lines.Add("/reset <all|symbol>");
                lines.Add(string.Empty);

                lines.Add("Help commands");
                lines.Add("/help");
                lines.Add(string.Empty);
            }

            await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), string.Join("\r\n", lines));
        }
    }
}