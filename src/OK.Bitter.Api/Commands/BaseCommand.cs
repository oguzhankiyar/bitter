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

        public BaseCommand(IServiceProvider serviceProvider)
        {
            if (serviceProvider == default)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _userRepository = serviceProvider.GetRequiredService<IUserRepository>();
        }

        public override Task OnPreExecutionAsync()
        {
            User = _userRepository.Get(x => x.ChatId == Context.ChatId);

            return base.OnPreExecutionAsync();
        }
    }
}