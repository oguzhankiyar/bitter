using OK.Bitter.Common.Entities;
using System.Collections.Generic;

namespace OK.Bitter.Core.Repositories
{
    public interface ISettingRepository
    {
        IEnumerable<SettingEntity> FindSettings(string userId);

        SettingEntity FindSetting(string userId, string key);

        SettingEntity InsertSetting(SettingEntity setting);

        bool UpdateSetting(SettingEntity setting);

        bool RemoveSetting(string settingId);
    }
}