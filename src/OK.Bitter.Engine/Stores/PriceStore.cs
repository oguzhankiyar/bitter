using System;
using System.Collections.Generic;
using System.Linq;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;

namespace OK.Bitter.Engine.Stores
{
    public class PriceStore : IStore<PriceModel>
    {
        public event EventHandler<PriceModel> OnInserted;
        public event EventHandler<PriceModel> OnUpdated;
        public event EventHandler<PriceModel> OnDeleted;

        private readonly IPriceManager _priceManager;

        private List<PriceModel> _items = new List<PriceModel>();

        public PriceStore(IPriceManager priceManager)
        {
            _priceManager = priceManager ?? throw new ArgumentNullException(nameof(priceManager));
        }

        public void Init()
        {
            _priceManager
                .GetLastPrices()
                .ForEach(Upsert);
        }

        public List<PriceModel> Get(Func<PriceModel, bool> expression = null)
        {
            if (expression == null)
            {
                return _items;
            }

            return _items.Where(expression).ToList();
        }

        public PriceModel Find(Func<PriceModel, bool> expression)
        {
            return _items.FirstOrDefault(expression);
        }

        public void Upsert(PriceModel price)
        {
            var item = _items.FirstOrDefault(x => x.SymbolId == price.SymbolId);
            if (item != null)
            {
                item.Price = price.Price;
                item.Date = price.Date;
                OnUpdated?.Invoke(this, price);
            }
            else
            {
                _items.Add(price);
                OnInserted?.Invoke(this, price);
            }
        }

        public void Delete(PriceModel price)
        {
            _items.RemoveAll(x => x.SymbolId == price.SymbolId);
            OnDeleted?.Invoke(this, price);
        }

        public void Delete(Func<PriceModel, bool> filter = null)
        {
            var prices = Get(filter);

            foreach (var price in prices)
            {
                _items.RemoveAll(x => x.SymbolId == price.SymbolId);
                OnDeleted?.Invoke(this, price);
            }
        }
    }
}