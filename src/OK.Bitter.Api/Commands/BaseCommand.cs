using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    public abstract class BaseCommand : CommandBase
    {
        public UserEntity User { get; private set; }

        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;

        public BaseCommand(IServiceProvider serviceProvider)
        {
            if (serviceProvider == default)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _userRepository = serviceProvider.GetRequiredService<IUserRepository>();
            _messageRepository = serviceProvider.GetRequiredService<IMessageRepository>();
        }

        public override Task OnPreExecutionAsync()
        {
            User = _userRepository.Get(x => x.ChatId == Context.ChatId);

            _messageRepository.Save(new MessageEntity()
            {
                UserId = User?.Id,
                ChatId = Context.ChatId.ToString(),
                Text = Context.MessageText,
                Date = DateTime.UtcNow
            });

            return base.OnPreExecutionAsync();
        }
    }
}