using System;
using System.Threading.Tasks;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("message")]
    public class MessageCommand : BaseCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageService _messageService;

        public MessageCommand(
            IUserRepository userRepository,
            IMessageService messageService,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        [CommandCase("{message}")]
        public async Task SendAsync(string message)
        {
            if (User.Type != UserTypeEnum.Admin)
            {
                await ReplyAsync("Unauthorized!");

                return;
            }

            var users = _userRepository.GetList();

            foreach (var item in users)
            {
                await _messageService.SendMessageAsync(item.ChatId, message);
            }
        }
    }
}