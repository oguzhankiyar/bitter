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
using OK.Bitter.Engine.Constants;

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
            InitSymbolsAsync().Wait();

            return _symbolRepository
                .GetList()
                .Select(x => new SymbolModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    FriendlyName = x.FriendlyName,
                    Base = x.Base,
                    Quote = x.Quote,
                    Route = x.Route,
                    MinimumChange = x.MinimumChange
                })
                .ToList();
        }

        private async Task InitSymbolsAsync()
        {
            var entities = new List<SymbolEntity>();

            var symbols = await GetSymbolsAsync();
            var prices = await GetPricesAsync();

            var bases = symbols.Select(x => x.Base);
            var quotes = symbols.Select(x => x.Quote);
            var uniques = bases.Concat(quotes).Distinct();

            var minChanges = GetMinimumChanges(uniques, symbols, prices);

            foreach (var unique in uniques)
            {
                foreach (var mainCurrency in SymbolConstants.MainCurrencies)
                {
                    var route = GetShortestRoute(symbols.Select(x => (x.Base, x.Quote)), unique, mainCurrency, mainCurrency)
                        .Select(x => new { x.Base, x.Quote, x.IsReverse });
                    if (route.Any())
                    {
                        entities.Add(new SymbolEntity
                        {
                            Name = $"{unique}{SymbolConstants.SymbolSeparator}{mainCurrency}",
                            FriendlyName = string.Concat(unique, mainCurrency),
                            Base = unique,
                            Quote = mainCurrency,
                            Route = JsonSerializer.Serialize(route),
                            MinimumChange = minChanges[unique]
                        });
                    }
                }
            }

            foreach (var entity in entities)
            {
                var exist = _symbolRepository.Get(x => x.Name == entity.Name);
                if (exist != null)
                {
                    entity.Id = exist.Id;
                }

                _symbolRepository.Save(entity);
            }
        }

        private async Task<IEnumerable<(string Base, string Quote, decimal TickSize)>> GetSymbolsAsync()
        {
            var list = new List<(string Base, string Quote, decimal TickSize)>();

            var url = "https://api.binance.com/api/v3/exchangeInfo";

            var response = await new HttpClient().GetAsync(url);

            var content = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            var array = root.GetProperty("symbols").EnumerateArray();

            foreach (var item in array)
            {
                var status = item.GetProperty("status").GetString();
                if (status != "TRADING")
                {
                    continue;
                }

                var permissions = item.GetProperty("permissions").EnumerateArray();
                if (!permissions.Any(x => x.GetString() == "SPOT"))
                {
                    continue;
                }

                var symbol = item.GetProperty("symbol").GetString();
                var baseCurrency = item.GetProperty("baseAsset").GetString();
                var quoteCurrency = item.GetProperty("quoteAsset").GetString();
                var tickSize = decimal.Parse(item.GetProperty("filters")
                    .EnumerateArray()
                    .FirstOrDefault(x => x.GetProperty("filterType").GetString() == "PRICE_FILTER")
                    .GetProperty("tickSize").GetString());

                list.Add((baseCurrency, quoteCurrency, tickSize));
            }

            return list;
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
                var baseCurrency = item.GetProperty("symbol").GetString();
                var quoteCurrency = decimal.Parse(item.GetProperty("price").GetString());

                list.Add((baseCurrency, quoteCurrency));
            }

            return list;
        }

        private List<(string Base, string Quote, bool IsReverse)> GetShortestRoute(IEnumerable<(string Base, string Quote)> symbols, string baseCurrency, string quoteCurrency, string mainCurrency)
        {
            var baseExist = symbols.FirstOrDefault(x => x.Base == baseCurrency && x.Quote == quoteCurrency);
            if (baseExist != default)
            {
                return new List<(string Base, string Quote, bool IsReverse)>
                {
                    (baseExist.Base, baseExist.Quote, false)
                };
            }

            var quoteExist = symbols.FirstOrDefault(x => x.Base == quoteCurrency && x.Quote == baseCurrency);
            if (quoteExist != default)
            {
                return new List<(string Base, string Quote, bool IsReverse)>
                {
                    (quoteExist.Base, quoteExist.Quote, true)
                };
            }

            var allRoutes = new List<List<(string Base, string Quote, bool IsReverse)>>();

            if (baseCurrency != mainCurrency)
            {
                foreach (var symbol in symbols.Where(x => x.Base == baseCurrency))
                {
                    var current = (symbol.Base, symbol.Quote, false);

                    var routes = new List<(string Base, string Quote, bool IsReverse)> { current };
                    var route = GetShortestRoute(symbols, symbol.Quote, quoteCurrency, mainCurrency);

                    if (route.Any() && route.Last().Quote == quoteCurrency)
                    {
                        routes.AddRange(route);
                        allRoutes.Add(routes);
                    }
                }
            }

            if (quoteCurrency != mainCurrency)
            {
                foreach (var symbol in symbols.Where(x => x.Base == quoteCurrency))
                {
                    var current = (symbol.Base, symbol.Quote, true);

                    var routes = new List<(string Base, string Quote, bool IsReverse)> { current };
                    var route = GetShortestRoute(symbols, symbol.Quote, baseCurrency, mainCurrency);

                    if (route.Any() && route.Last().Quote == baseCurrency)
                    {
                        routes.AddRange(route);
                        allRoutes.Add(routes);
                    }
                }
            }

            if (!allRoutes.Any())
            {
                return new List<(string Base, string Quote, bool IsReverse)>();
            }

            return allRoutes.OrderBy(x => x.Count).First();
        }

        private IDictionary<string, decimal> GetMinimumChanges(IEnumerable<string> uniques, IEnumerable<(string Base, string Quote, decimal TickSize)> symbols, IEnumerable<(string Symbol, decimal Price)> prices)
        {
            var symbolChanges = symbols
                   .Select(symbol =>
                   {
                       var minChange = 0.01m;

                       var price = prices.FirstOrDefault(x => x.Symbol == string.Concat(symbol.Base, symbol.Quote));
                       if (price != default)
                       {
                           minChange = symbol.TickSize / price.Price;
                           minChange = Math.Round(minChange * 100) / 100m;
                           minChange = Math.Max(minChange, 0.01m);
                       }

                       return new { Currency = symbol.Base, Change = minChange };
                   });

            var minChanges = uniques
                .Select(unique =>
                {
                    var exist = symbolChanges
                        .GroupBy(y => y.Currency)
                        .Where(y => y.Key == unique);

                    if (!exist.Any())
                    {
                        return new { Currency = unique, Change = 0.01m };
                    }

                    return new { Currency = unique, Change = exist.Max(z => z.Max(t => t.Change)) };
                });

            return minChanges.ToDictionary(x => x.Currency, x => x.Change);
        }
    }
}