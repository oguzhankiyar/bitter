using OK.Bitter.Common.Entities;
using System.Collections.Generic;

namespace OK.Bitter.Core.Repositories
{
    public interface ISymbolRepository
    {
        IEnumerable<SymbolEntity> FindSymbols();
        
        SymbolEntity InsertSymbol(SymbolEntity symbol);
    }
}