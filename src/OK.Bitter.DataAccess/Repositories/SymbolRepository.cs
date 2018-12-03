using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;
using System.Collections.Generic;

namespace OK.Bitter.DataAccess.Repositories
{
    public class SymbolRepository : BaseRepository<SymbolEntity>, ISymbolRepository
    {
        public SymbolRepository(BitterDataContext context) : base(context, "Symbols")
        {
        }

        public IEnumerable<SymbolEntity> FindSymbols()
        {
            return GetList();
        }

        public SymbolEntity InsertSymbol(SymbolEntity symbol)
        {
            return Save(symbol);
        }
    }
}