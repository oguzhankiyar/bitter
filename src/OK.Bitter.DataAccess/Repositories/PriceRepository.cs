using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;

namespace OK.Bitter.DataAccess.Repositories
{
    public class PriceRepository : BaseRepository<PriceEntity>, IPriceRepository
    {
        public PriceRepository(BitterDataContext context) : base(context.Prices)
        {

        }
    }
}