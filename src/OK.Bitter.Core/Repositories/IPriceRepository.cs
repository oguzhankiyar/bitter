using OK.Bitter.Common.Entities;
using System;
using System.Collections.Generic;

namespace OK.Bitter.Core.Repositories
{
    public interface IPriceRepository
    {
        IEnumerable<PriceEntity> FindPrices();

        IEnumerable<PriceEntity> FindPrices(DateTime startDate);

        IEnumerable<PriceEntity> FindPrices(string symbolId, DateTime startDate);

        PriceEntity InsertPrice(PriceEntity price);
    }
}