using OK.Bitter.Common.Models;
using System.Collections.Generic;

namespace OK.Bitter.Core.Managers
{
    public interface ISymbolManager
    {
        List<SymbolModel> GetSymbols();
    }
}