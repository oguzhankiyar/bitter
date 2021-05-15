using System;
using System.Collections.Generic;
using System.Linq;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;

namespace OK.Bitter.Engine.Stores
{
    public class SymbolStore : IStore<SymbolModel>
    {
        public event EventHandler<SymbolModel> OnInserted;
        public event EventHandler<SymbolModel> OnUpdated;
        public event EventHandler<SymbolModel> OnDeleted;

        private readonly ISymbolManager _symbolManager;

        private List<SymbolModel> _items = new List<SymbolModel>();

        public SymbolStore(ISymbolManager symbolManager)
        {
            _symbolManager = symbolManager ?? throw new ArgumentNullException(nameof(symbolManager));
        }

        public void Init()
        {
            _symbolManager
                .GetSymbols()
                .ForEach(Upsert);
        }

        public List<SymbolModel> Get(Func<SymbolModel, bool> expression = null)
        {
            if (expression == null)
            {
                return _items;
            }

            return _items.Where(expression).ToList();
        }

        public SymbolModel Find(Func<SymbolModel, bool> expression)
        {
            return _items.FirstOrDefault(expression);
        }

        public void Upsert(SymbolModel symbol)
        {
            var item = _items.FirstOrDefault(x => x.Id == symbol.Id);
            if (item != null)
            {
                item.Name = symbol.Name;
                item.FriendlyName = symbol.FriendlyName;
                item.MinimumChange = symbol.MinimumChange;
                OnUpdated?.Invoke(this, symbol);
            }
            else
            {
                _items.Add(symbol);
                OnInserted?.Invoke(this, symbol);
            }
        }

        public void Delete(SymbolModel symbol)
        {
            _items.RemoveAll(x => x.Id == symbol.Id);
            OnDeleted?.Invoke(this, symbol);
        }

        public void Delete(Func<SymbolModel, bool> filter = null)
        {
            var symbols = Get(filter);

            foreach (var symbol in symbols)
            {
                _items.RemoveAll(x => x.Id == symbol.Id);
                OnDeleted?.Invoke(this, symbol);
            }
        }
    }
}