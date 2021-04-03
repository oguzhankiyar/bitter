using System;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("auth")]
    public class AuthCommand : BaseCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageService _messageService;
        private readonly ISocketServiceManager _socketServiceManager;

        public AuthCommand(
            IUserRepository userRepository,
            IMessageService messageService,
            ISocketServiceManager socketServiceManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _socketServiceManager = socketServiceManager ?? throw new ArgumentNullException(nameof(socketServiceManager));
        }

        [CommandCase("{key}")]
        public async Task LoginAsync(string key)
        {
            var user = _userRepository.FindUser(Context.ChatId);

            if (user == null)
            {
                user = new UserEntity();
            }

            user.ChatId = Context.ChatId;
            user.Username = Context.Username;
            user.FirstName = Context.FirstName;
            user.LastName = Context.LastName;

            if (key != "normal!123" && key != "admin!123")
            {
                await ReplyAsync("Invalid key!");

                return;
            }

            if (key == "normal!123")
            {
                user.Type = UserTypeEnum.Normal;
            }
            else if (key == "admin!123")
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
                await _messageService.SendMessageAsync(admin.ChatId, $"{Context.Username} is added to users!");
            }
        }
    }
}