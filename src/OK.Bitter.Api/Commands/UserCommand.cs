using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Engine.Stores;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("users")]
    public class UserCommand : BaseCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IStore<UserModel> _userStore;
        private readonly IStore<SubscriptionModel> _subscriptionStore;
        private readonly IStore<AlertModel> _alertStore;

        public UserCommand(
            IUserRepository userRepository,
            ISubscriptionRepository subscriptionRepository,
            IAlertRepository alertRepository,
            IStore<UserModel> userStore,
            IStore<SubscriptionModel> subscriptionStore,
            IStore<AlertModel> alertStore,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _alertRepository = alertRepository ?? throw new ArgumentNullException(nameof(alertRepository));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _subscriptionStore = subscriptionStore ?? throw new ArgumentNullException(nameof(subscriptionStore));
            _alertStore = alertStore ?? throw new ArgumentNullException(nameof(alertStore));
        }

        [CommandCase("get", "{name}")]
        public async Task GetAsync(string name)
        {
            if (User.Type != UserTypeEnum.Admin)
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

        [CommandCase("del", "{name}")]
        public async Task DelAsync(string name)
        {
            if (User.Type != UserTypeEnum.Admin)
            {
                await ReplyAsync("Unauthorized!");

                return;
            }

            var userEntity = _userRepository.Get(x => x.Username == name);
            if (userEntity == null)
            {
                await ReplyAsync("User is not found!");

                return;
            }

            _subscriptionRepository.Delete(x => x.UserId == userEntity.Id);
            _alertRepository.Delete(x => x.UserId == userEntity.Id);
            _userRepository.Delete(userEntity.Id);

            _subscriptionStore.Delete(x => x.UserId == userEntity.Id);
            _alertStore.Delete(x => x.UserId == userEntity.Id);
            _userStore.Delete(x => x.Id == userEntity.Id);

            await ReplyAsync("Success!");
        }
    }
}