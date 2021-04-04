using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;

namespace OK.Bitter.DataAccess.Repositories
{
    public class TradeRepository : BaseRepository<TradeEntity>, ITradeRepository
    {
        public TradeRepository(BitterDataContext context) : base(context.Trades)
        {

        }
    }
}