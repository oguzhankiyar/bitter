using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OK.Bitter.Api.Config;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using OK.Bitter.Engine.Stores;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("auth")]
    public class AuthCommand : BaseCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IStore<UserModel> _userStore;
        private readonly IMessageService _messageService;
        private readonly BitterConfig _bitterConfig;

        public AuthCommand(
            IUserRepository userRepository,
            IStore<UserModel> userStore,
            IMessageService messageService,
            BitterConfig bitterConfig,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _bitterConfig = bitterConfig ?? throw new ArgumentNullException(nameof(bitterConfig));
        }

        [CommandCase("{key}")]
        public async Task LoginAsync(string key)
        {
            var user = _userRepository.Get(x => x.ChatId == Context.ChatId);

            if (user == null)
            {
                user = new UserEntity();
            }

            user.ChatId = Context.ChatId;
            user.Username = Context.Username;
            user.FirstName = Context.FirstName;
            user.LastName = Context.LastName;

            key = HashKey(key);

            if (!_bitterConfig.Passwords.Values.Contains(key))
            {
                await ReplyAsync("Invalid key!");

                return;
            }

            user.Type = _bitterConfig.Passwords.FirstOrDefault(x => x.Value == key).Key;

            _userRepository.Save(user);
            _userStore.Upsert(new UserModel
            {
                Id = user.Id,
                ChatId = user.ChatId
            });

            await ReplyAsync("Success!");

            var admins = _userRepository.GetList(x => x.Type == UserTypeEnum.Admin);

            foreach (var admin in admins)
            {
                await _messageService.SendMessageAsync(admin.ChatId, $"{Context.Username} is added to users!");
            }
        }

        private string HashKey(string key)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(key);
                var hashBytes = md5.ComputeHash(inputBytes);

                var sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}