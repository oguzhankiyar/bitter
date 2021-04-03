using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;

namespace OK.Bitter.DataAccess.Repositories
{
    public class SymbolRepository : BaseRepository<SymbolEntity>, ISymbolRepository
    {
        public SymbolRepository(BitterDataContext context) : base(context.Symbols)
        {

        }
    }
}