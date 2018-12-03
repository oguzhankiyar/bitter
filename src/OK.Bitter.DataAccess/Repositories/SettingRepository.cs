using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;
using System.Collections.Generic;

namespace OK.Bitter.DataAccess.Repositories
{
    public class SettingRepository : BaseRepository<SettingEntity>, ISettingRepository
    {
        public SettingRepository(BitterDataContext context) : base(context, "Settings")
        {
        }

        public IEnumerable<SettingEntity> FindSettings(string userId)
        {
            return GetList(x => x.UserId == userId);
        }

        public SettingEntity FindSetting(string userId, string key)
        {
            return Get(x => x.UserId == userId && x.Key == key);
        }

        public SettingEntity InsertSetting(SettingEntity setting)
        {
            return Save(setting);
        }

        public bool UpdateSetting(SettingEntity setting)
        {
            Save(setting);

            return true;
        }

        public bool RemoveSetting(string settingId)
        {
            Delete(settingId);

            return true;
        }
    }
}