using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{

    public class SettingCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly ISettingRepository _settingRepository;
        private readonly IMessageService _messageService;

        public SettingCommand(IUserRepository userRepository,
                              ISettingRepository settingRepository,
                              IMessageService messageService)
        {
            _userRepository = userRepository;
            _settingRepository = settingRepository;
            _messageService = messageService;
        }

        public async Task ExecuteAsync(BotUpdateInput input)
        {
            string message = input.Message.Text.Trim();

            message = message.Replace((message + " ").Split(' ')[0], string.Empty).Trim();

            List<string> values = message.Contains(" ") ? message.Split(' ').ToList() : new List<string>() { message };

            UserEntity user = _userRepository.FindUser(input.Message.Chat.Id.ToString());

            if (user == null)
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Unauthorized!");

                return;
            }

            // "/settings get <all|key>"
            if (values[0] == "get")
            {
                if (values.Count == 1)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                if (values[1] == "all")
                {
                    var settings = _settingRepository.FindSettings(user.Id);

                    List<string> lines = new List<string>();

                    foreach (var item in settings)
                    {
                        lines.Add($"{item.Key} = {item.Value}");
                    }

                    if (!lines.Any())
                    {
                        await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "There are no settings!");

                        return;
                    }

                    lines = lines.OrderBy(x => x).ToList();

                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), string.Join("\r\n", lines));

                    return;
                }
                else
                {
                    var setting = _settingRepository.FindSetting(user.Id, values[1]);

                    if (setting == null)
                    {
                        await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Setting is not found!");

                        return;
                    }

                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), $"{setting.Key} = {setting.Value}");

                    return;
                }
            }
            // "/settings set <key> <value>"
            else if (values[0] == "set")
            {
                if (values.Count < 3)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                var setting = _settingRepository.FindSetting(user.Id, values[1]);

                if (setting == null)
                {
                    _settingRepository.InsertSetting(new SettingEntity()
                    {
                        UserId = user.Id,
                        Key = values[1],
                        Value = values[2]
                    });
                }
                else
                {
                    setting.Value = values[2];

                    _settingRepository.UpdateSetting(setting);
                }

                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Success!");
            }
            // "/settings del <key>"
            else if (values[0] == "del")
            {
                if (values.Count == 1)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                var setting = _settingRepository.FindSetting(user.Id, values[1]);

                if (setting == null)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Setting is not found!");

                    return;
                }

                _settingRepository.RemoveSetting(setting.Id);

                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Success!");
            }
            else
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                return;
            }
        }
    }
}