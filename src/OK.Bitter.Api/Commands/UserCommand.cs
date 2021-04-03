using System;
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
                string result = string.Empty;

                var users = _userRepository.FindUsers();

                foreach (var item in users)
                {
                    result += $"@{item.Username} - {item.FirstName} {item.LastName}\r\n";
                }

                await ReplyAsync(result);
            }
            else
            {
                var userEntity = _userRepository.FindUser(name);
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