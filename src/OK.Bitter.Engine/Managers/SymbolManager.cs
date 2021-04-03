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
    public class SymbolManager : ISymbolManager
    {
        private readonly ISymbolRepository _symbolRepository;

        public SymbolManager(ISymbolRepository symbolRepository)
        {
            _symbolRepository = symbolRepository;
        }

        public List<SymbolModel> GetSymbols()
        {
            InitSymbols();

            return _symbolRepository
                .GetList()
                .Select(x => new SymbolModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    FriendlyName = x.FriendlyName,
                    MinimumChange = x.MinimumChange
                })
                .ToList();
        }

        private void InitSymbols()
        {
            var symbols = _symbolRepository.GetList();

            List<string> importantSymbols = new List<string>() { "BTC|USDT" };

            foreach (var symbol in importantSymbols)
            {
                string friendlyName = symbol.Split('|')[0];

                if (!symbols.Any(x => x.FriendlyName == friendlyName))
                {
                    _symbolRepository.Save(new SymbolEntity()
                    {
                        Name = symbol,
                        FriendlyName = friendlyName,
                        MinimumChange = (decimal)0.005
                    });
                }
            }

            foreach (var symbol in GetAllSymbols().Where(x => !importantSymbols.Any(y => y == x)))
            {
                string friendlyName = symbol.Split('|')[0];

                if (!symbols.Any(x => x.FriendlyName == friendlyName))
                {
                    _symbolRepository.Save(new SymbolEntity()
                    {
                        Name = symbol,
                        FriendlyName = friendlyName,
                        MinimumChange = (decimal)0.01
                    });
                }
            }
        }

        private List<string> GetAllSymbols()
        {
            List<string> symbols = new List<string>();

            string url = "https://api.binance.com/api/v1/exchangeInfo";

            string result = new HttpClient().GetAsync(url).Result.Content.ReadAsStringAsync().Result;

            var json = JsonDocument.Parse(result);
            var root = json.RootElement;

            var array = root.GetProperty("symbols").EnumerateArray();

            foreach (var item in array)
            {
                if (item.GetProperty("quoteAsset").GetString() != "BTC")
                {
                    continue;
                }

                symbols.Add(item.GetProperty("baseAsset").GetString() + "|" + item.GetProperty("quoteAsset").GetString());
            }

            return symbols;
        }
    }
}