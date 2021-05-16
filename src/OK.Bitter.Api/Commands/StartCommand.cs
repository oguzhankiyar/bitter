using System;
using System.Text;
using System.Threading.Tasks;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using OK.Bitter.Engine.Stores;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("/start|start")]
    public class StartCommand : BaseCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IStore<UserModel> _userStore;
        private readonly IMessageService _messageService;

        public StartCommand(
            IUserRepository userRepository,
            IStore<UserModel> userStore,
            IMessageService messageService,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        [CommandCase]
        public async Task StartAsync()
        {
            var user = _userRepository.Get(x => x.ChatId == Context.ChatId);

            if (user == null)
            {
                user = new UserEntity
                {
                    Type = UserTypeEnum.Normal
                };
            }

            user.ChatId = Context.ChatId;
            user.Username = Context.Username;
            user.FirstName = Context.FirstName;
            user.LastName = Context.LastName;

            _userRepository.Save(user);
            _userStore.Upsert(new UserModel
            {
                Id = user.Id,
                ChatId = user.ChatId
            });

            var reply = new StringBuilder();

            if (string.IsNullOrEmpty(Context.Username))
            {
                reply.AppendLine("Wellcome!");
            }
            else
            {
                reply.AppendLine($"Wellcome @{Context.Username}!");
            }

            reply.AppendLine("You can find all commands by typing help.");

            await ReplyAsync(reply.ToString());

            var admins = _userRepository.GetList(x => x.Type == UserTypeEnum.Admin);

            foreach (var admin in admins)
            {
                await _messageService.SendMessageAsync(admin.ChatId, $"@{Context.Username} is joined!");
            }
        }
    }
}