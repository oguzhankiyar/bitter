using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("settings")]
    public class SettingCommand : BaseCommand
    {
        private readonly ISettingRepository _settingRepository;

        public SettingCommand(
            ISettingRepository settingRepository,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
        }

        [CommandCase("get", "{key}")]
        public async Task GetAsync(string key)
        {
            if (key == "all")
            {
                var settings = _settingRepository.GetList(x => x.UserId == User.Id).OrderBy(x => x.Key);
                if (!settings.Any())
                {
                    await ReplyAsync("There are no settings!");

                    return;
                }

                var lines = new List<string>();

                foreach (var item in settings)
                {
                    lines.Add($"{item.Key} = {item.Value}");
                }

                await ReplyPaginatedAsync(lines);
            }
            else
            {
                var setting = _settingRepository.Get(x => x.UserId == User.Id && x.Key == key);
                if (setting == null)
                {
                    await ReplyAsync("Setting is not found!");

                    return;
                }

                await ReplyAsync($"{setting.Key} = {setting.Value}");

                return;
            }
        }

        [CommandCase("set", "{key}", "{value}")]
        public async Task SetAsync(string key, string value)
        {
            var setting = _settingRepository.Get(x => x.UserId == User.Id && x.Key == key);
            if (setting == null)
            {
                _settingRepository.Save(new SettingEntity()
                {
                    UserId = User.Id,
                    Key = key,
                    Value = value
                });
            }
            else
            {
                setting.Value = value;

                _settingRepository.Save(setting);
            }

            await ReplyAsync("Success!");
        }

        [CommandCase("del", "{key}")]
        public async Task DelAsync(string key)
        {
            var setting = _settingRepository.Get(x => x.UserId == User.Id && x.Key == key);
            if (setting == null)
            {
                await ReplyAsync("Setting is not found!");

                return;
            }

            _settingRepository.Delete(setting.Id);

            await ReplyAsync("Success!");
        }
    }
}