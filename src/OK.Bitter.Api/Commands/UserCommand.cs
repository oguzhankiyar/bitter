using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public class UserCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageService _messageService;

        public UserCommand(IUserRepository userRepository,
                           IMessageService messageService)
        {
            _userRepository = userRepository;
            _messageService = messageService;
        }

        public async Task ExecuteAsync(BotUpdateInput input)
        {
            string message = input.Message.Text.Trim();

            message = message.Replace((message + " ").Split(' ')[0], string.Empty).Trim();

            List<string> values = message.Contains(" ") ? message.Split(' ').ToList() : new List<string>() { message };

            UserEntity user = _userRepository.FindUser(input.Message.Chat.Id.ToString());

            if (user == null || user.Type != UserTypeEnum.Admin)
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Unauthorized!");

                return;
            }

            if (values[0] == "get")
            {
                if (values.Count == 1)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                if (values[1] == "all")
                {
                    string result = string.Empty;

                    var users = _userRepository.FindUsers();

                    foreach (var item in users)
                    {
                        result += $"@{item.Username} - {item.FirstName} {item.LastName}\r\n";
                    }

                    await _messageService.SendMessageAsync(user.ChatId, result);
                }
                else
                {
                    var usr = _userRepository.FindUser(values[1]);

                    if (usr == null)
                    {
                        await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "User is not found!");

                        return;
                    }

                    await _messageService.SendMessageAsync(user.ChatId, $"@{usr.Username} - {usr.FirstName} {usr.LastName}");
                }
            }
            else
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                return;
            }
        }
    }
}