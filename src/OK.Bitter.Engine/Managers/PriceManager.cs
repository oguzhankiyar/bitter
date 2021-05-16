using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;

namespace OK.Bitter.Engine.Managers
{
    public class PriceManager : IPriceManager
    {
        private readonly IPriceRepository _priceRepository;
        private readonly ISymbolManager _symbolManager;

        public PriceManager(IPriceRepository priceRepository, ISymbolManager symbolManager)
        {
            _priceRepository = priceRepository;
            _symbolManager = symbolManager;
        }

        public List<PriceModel> GetLastPrices()
        {
            var lastPrices = _priceRepository
                .GetList()
                .GroupBy(x => x.SymbolId)
                .Select(x => x.OrderByDescending(y => y.Date).Last())
                .Select(x => new PriceModel()
                {
                    SymbolId = x.SymbolId,
                    Price = x.Price,
                    Date = x.Date
                })
                .ToList();

            var symbols = _symbolManager.GetSymbols();
            var prices = GetPricesAsync().Result;

            foreach (var symbol in symbols)
            {
                var lastPrice = lastPrices.FirstOrDefault(x => x.SymbolId == symbol.Id);

                if (lastPrice == null)
                {
                    var latest = prices.FirstOrDefault(x => x.Symbol == string.Concat(symbol.Base, symbol.Quote));
                    if (latest == default)
                    {
                        continue;
                    }

                    var symbolId = symbol.Id;
                    var price = latest.Price;
                    var date = DateTime.UtcNow;

                    lastPrices.Add(new PriceModel()
                    {
                        SymbolId = symbolId,
                        Price = price,
                        Date = date
                    });

                    SaveLastPrice(symbolId, price, 0, date, date);
                }
            }

            return lastPrices;
        }

        public bool SaveLastPrice(string symbolId, decimal price, decimal change, DateTime lastChange, DateTime date)
        {
            try
            {
                _priceRepository.Save(new PriceEntity()
                {
                    SymbolId = symbolId,
                    Price = price,
                    Change = change,
                    LastChangeDate = lastChange,
                    Date = date
                });
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool RemoveOldPrices(DateTime startDate)
        {
            _priceRepository.Delete(x => x.Date < startDate);
            return true;
        }

        public async Task<IEnumerable<(string Symbol, decimal Price)>> GetPricesAsync()
        {
            var list = new List<(string Symbol, decimal Price)>();

            var url = "https://api.binance.com/api/v3/ticker/price";

            var response = await new HttpClient().GetAsync(url);

            var content = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            var array = root.EnumerateArray();

            foreach (var item in array)
            {
                var symbol = item.GetProperty("symbol").GetString();
                var price = decimal.Parse(item.GetProperty("price").GetString());

                list.Add((symbol, price));
            }

            return list;
        }
    }
}