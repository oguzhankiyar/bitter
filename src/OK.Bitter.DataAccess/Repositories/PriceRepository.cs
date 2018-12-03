using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;
using System;
using System.Collections.Generic;

namespace OK.Bitter.DataAccess.Repositories
{
    public class PriceRepository : BaseRepository<PriceEntity>, IPriceRepository
    {
        public PriceRepository(BitterDataContext context) : base(context, "Prices")
        {
        }

        public IEnumerable<PriceEntity> FindPrices()
        {
            return GetList();
        }
        
        public IEnumerable<PriceEntity> FindPrices(DateTime startDate)
        {
            return GetList(x => x.CreatedDate >= startDate);
        }


        public IEnumerable<PriceEntity> FindPrices(string symbolId, DateTime startDate)
        {
            return GetList(x => x.SymbolId == symbolId && x.Date >= startDate);
        }

        public PriceEntity InsertPrice(PriceEntity price)
        {
            return Save(price);
        }
    }
}