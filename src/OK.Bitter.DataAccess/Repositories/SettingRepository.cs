using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;

namespace OK.Bitter.DataAccess.Repositories
{
    public class SettingRepository : BaseRepository<SettingEntity>, ISettingRepository
    {
        public SettingRepository(BitterDataContext context) : base(context.Settings)
        {

        }
    }
}