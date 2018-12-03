using OK.Bitter.Common.Models;
using System;
using System.Collections.Generic;

namespace OK.Bitter.Core.Managers
{
    public interface IPriceManager
    {
        List<PriceModel> GetLastPrices();

        bool SaveLastPrice(string symbolId, decimal price, decimal change, DateTime lastChange, DateTime date);
    }
}