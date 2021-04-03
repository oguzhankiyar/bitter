using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;

namespace OK.Bitter.DataAccess.Repositories
{
    public class AlertRepository : BaseRepository<AlertEntity>, IAlertRepository
    {
        public AlertRepository(BitterDataContext context) : base(context.Alerts)
        {

        }
    }
}