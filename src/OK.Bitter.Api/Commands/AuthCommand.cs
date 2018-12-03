using OK.Bitter.Api.HostedServices;
using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public class AuthCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageService _messageService;
        private readonly ISocketHostedService _socketService;
        private readonly ISocketServiceManager _socketServiceManager;

        public AuthCommand(IUserRepository userRepository,
                           IMessageService messageService,
                           ISocketHostedService socketService,
                           ISocketServiceManager socketServiceManager)
        {
            _userRepository = userRepository;
            _messageService = messageService;
            _socketService = socketService;
            _socketServiceManager = socketServiceManager;
        }

        public async Task ExecuteAsync(BotUpdateInput input)
        {
            string message = input.Message.Text.Trim();

            message = message.Replace((message + " ").Split(' ')[0], string.Empty).Trim();

            List<string> values = message.Contains(" ") ? message.Split(' ').ToList() : new List<string>() { message };

            UserEntity user = _userRepository.FindUser(input.Message.Chat.Id.ToString());

            if (user == null)
            {
                user = new UserEntity();
            }

            user.ChatId = input.Message.Chat.Id.ToString();
            user.Username = input.Message.From.Username;
            user.FirstName = input.Message.From.FirstName;
            user.LastName = input.Message.From.LastName;

            if (values[0] != "normal!123" && values[0] != "admin!123")
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid key!");
            }

            if (values[0] == "normal!123")
            {
                user.Type = UserTypeEnum.Normal;
            }
            else if (values[0] == "admin!123")
            {
                user.Type = UserTypeEnum.Admin;
            }

            if (string.IsNullOrEmpty(user.Id))
            {
                _userRepository.InsertUser(user);
            }
            else
            {
                _userRepository.UpdateUser(user);
            }

            _socketServiceManager.UpdateUsers();

            await _messageService.SendMessageAsync(user.ChatId, "Success!");

            var admins = _userRepository.FindUsers().Where(x => x.Type == UserTypeEnum.Admin);

            foreach (var admin in admins)
            {
                await _messageService.SendMessageAsync(admin.ChatId, $"{input.Message.From.Username} is added to users!");
            }
        }
    }
}