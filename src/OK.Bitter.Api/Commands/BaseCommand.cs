using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    public abstract class BaseCommand : CommandBase
    {
        protected UserEntity User { get; private set; }

        private readonly IUserRepository _userRepository;

        protected BaseCommand(IServiceProvider serviceProvider)
        {
            if (serviceProvider == default)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _userRepository = serviceProvider.GetRequiredService<IUserRepository>();
        }

        public override async Task OnPreExecutionAsync()
        {
            User = _userRepository.Get(x => x.ChatId == Context.ChatId);

            if (User == null)
            {
                await ReplyAsync("Please /start firstly.");

                await AbortAsync();
            }

            await base.OnPreExecutionAsync();
        }

        protected async Task ReplyPaginatedAsync(List<string> lines)
        {
            const int take = 25;
            
            var skip = 0;

            while (skip < lines.Count)
            {
                var items = lines.Skip(skip).Take(take);
                await ReplyAsync(string.Join("\r\n", items));
                await Task.Delay(500);

                skip += take;
            }
        }
    }
}