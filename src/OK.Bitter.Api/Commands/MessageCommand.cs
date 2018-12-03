using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public class MessageCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageService _messageService;

        public MessageCommand(IUserRepository userRepository,
                              IMessageService messageService)
        {
            _userRepository = userRepository;
            _messageService = messageService;
        }

        public async Task ExecuteAsync(BotUpdateInput input)
        {
            string message = input.Message.Text.Trim();

            message = message.Replace((message + " ").Split(' ')[0], string.Empty).Trim();

            UserEntity user = _userRepository.FindUser(input.Message.Chat.Id.ToString());

            if (user == null || user.Type != UserTypeEnum.Admin)
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Unauthorized!");

                return;
            }

            var users = _userRepository.FindUsers();

            foreach (var item in users)
            {
                await _messageService.SendMessageAsync(item.ChatId, message);
            }
        }
    }
}