using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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

            foreach (var symbol in symbols)
            {
                var lastPrice = lastPrices.FirstOrDefault(x => x.SymbolId == symbol.Id);

                if (lastPrice == null)
                {
                    string symbolId = symbol.Id;
                    decimal price = GetLatestPrice(symbol);
                    DateTime date = DateTime.Now;

                    lastPrices.Add(new PriceModel()
                    {
                        SymbolId = symbolId,
                        Price = price,
                        Date = date
                    });

                    SaveLastPrice(symbolId, price, 0, DateTime.Now, date);
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

        private static decimal GetLatestPrice(SymbolModel symbol)
        {
            string url = "https://api.binance.com/api/v3/ticker/price?symbol=" + symbol.Name.Replace("|", string.Empty);

            string result = new HttpClient().GetAsync(url).Result.Content.ReadAsStringAsync().Result;

            var json = JsonDocument.Parse(result);
            var root = json.RootElement;

            return decimal.Parse(root.GetProperty("price").GetString());
        }
    }
}