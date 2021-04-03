using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System.Collections.Generic;
using System.Linq;

namespace OK.Bitter.Engine.Managers
{
    public class UserManager : IUserManager
    {
        private readonly IUserRepository _userRepository;
        private readonly ISettingRepository _settingRepository;
        private readonly IMessageService _messageService;
        private readonly ICallService _callService;

        public UserManager(IUserRepository userRepository, ISettingRepository settingRepository, IMessageService messageService, ICallService callService)
        {
            _userRepository = userRepository;
            _settingRepository = settingRepository;
            _messageService = messageService;
            _callService = callService;
        }

        public List<UserModel> GetUsers()
        {
            return _userRepository.FindUsers()
                                  .Select(x => new UserModel()
                                  {
                                      Id = x.Id,
                                      ChatId = x.ChatId
                                  })
                                  .ToList();
        }

        public bool SendMessage(string userId, string message)
        {
            var user = _userRepository.FindUserById(userId);

            if (user == null)
            {
                return false;
            }

            _messageService.SendMessageAsync(user.ChatId, message);

            return true;
        }

        public bool CallUser(string userId, string message)
        {
            var tokenSetting = _settingRepository.FindSetting(userId, "alert_token");

            if (tokenSetting != null)
            {
                var callSetting = _settingRepository.FindSetting(userId, "call_enabled");

                var trueValues = new string[] { "1", "true" };

                if (callSetting == null || trueValues.Contains(callSetting.Value))
                {
                    _callService.CallAsync(tokenSetting.Value, message);

                    return true;
                }
            }

            return false;
        }
    }
}