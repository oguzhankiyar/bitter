using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Repositories;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("users")]
    public class UserCommand : BaseCommand
    {
        private readonly IUserRepository _userRepository;

        public UserCommand(
            IUserRepository userRepository,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        [CommandCase("get", "{name}")]
        public async Task GetAsync(string name)
        {
            if (User == null || User.Type != UserTypeEnum.Admin)
            {
                await ReplyAsync("Unauthorized!");

                return;
            }

            if (name == "all")
            {
                var lines = new List<string>();

                var users = _userRepository.GetList();

                foreach (var item in users)
                {
                    lines.Add($"@{item.Username} - {item.FirstName} {item.LastName}");
                }

                var skip = 0;
                var take = 25;

                while (skip < lines.Count)
                {
                    var items = lines.Skip(skip).Take(take);
                    await ReplyAsync(string.Join("\r\n", items));
                    await Task.Delay(500);

                    skip += take;
                }
            }
            else
            {
                var userEntity = _userRepository.Get(x => x.Username == name);
                if (userEntity == null)
                {
                    await ReplyAsync("User is not found!");

                    return;
                }

                await ReplyAsync($"@{userEntity.Username} - {userEntity.FirstName} {userEntity.LastName}");
            }
        }
    }
}