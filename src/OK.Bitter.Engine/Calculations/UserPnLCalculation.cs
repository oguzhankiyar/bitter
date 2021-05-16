using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;
using OK.Bitter.Engine.Stores;

namespace OK.Bitter.Engine.Calculations
{
    public class UserPnLCalculation
    {
        private readonly IStore<SymbolModel> _symbolStore;
        private readonly IStore<PriceModel> _priceStore;
        private readonly IUserManager _userManager;

        public UserPnLCalculation(
            IStore<SymbolModel> symbolStore,
            IStore<PriceModel> priceStore,
            IUserManager userManager)
        {
            _symbolStore = symbolStore ?? throw new ArgumentNullException(nameof(symbolStore));
            _priceStore = priceStore ?? throw new ArgumentNullException(nameof(priceStore));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public Task CalculateAsync(List<TradeModel> trades)
        {
            var lines = new List<string>();

            var totalPnL = (decimal.Zero, decimal.Zero);
            var tradesBySymbol = trades.GroupBy(x => x.SymbolId);

            foreach (var tradeGroup in tradesBySymbol)
            {
                var symbol = _symbolStore.Find(x => x.Id == tradeGroup.Key);
                if (symbol == null)
                {
                    continue;
                }

                var price = _priceStore.Find(x => x.SymbolId == tradeGroup.Key);
                if (price == null)
                {
                    continue;
                }

                var buyTrades = tradeGroup
                    .Where(x => x.Type == TradeTypeEnum.Buy)
                    .OrderBy(x => x.Time)
                    .ToList();
                var sellTrades = tradeGroup
                    .Where(x => x.Type == TradeTypeEnum.Sell)
                    .OrderBy(x => x.Time)
                    .ToList();

                var realized = decimal.Zero;
                var unrealized = decimal.Zero;

                foreach (var buyTrade in buyTrades.ToList())
                {
                    if (sellTrades.Any())
                    {
                        var sellTrade = sellTrades.FirstOrDefault();
                        var volumeDiff = buyTrade.Volume - sellTrade.Volume;

                        if (volumeDiff == 0)
                        {
                            realized += sellTrade.Volume * (sellTrade.Price - buyTrade.Price);
                            buyTrades.Remove(buyTrade);
                            sellTrades.Remove(sellTrade);
                        }
                        else if (volumeDiff < 0)
                        {
                            realized += buyTrade.Volume * (sellTrade.Price - buyTrade.Price);
                            buyTrade.Volume = decimal.Zero;
                            sellTrade.Volume -= buyTrade.Volume;
                            buyTrades.Remove(buyTrade);
                        }
                        else if (volumeDiff > 0)
                        {
                            realized += sellTrade.Volume * (sellTrade.Price - buyTrade.Price);
                            buyTrade.Volume -= sellTrade.Volume;
                            sellTrade.Volume = decimal.Zero;
                            sellTrades.Remove(sellTrade);
                        }
                    }
                }

                var buyVolume = buyTrades.Sum(x => x.Volume);
                var buyPrice = buyTrades.Any() ? buyTrades.Average(x => x.Price) : 0;
                var sellVolume = sellTrades.Sum(x => x.Volume);

                var openVolume = buyVolume - sellVolume;

                unrealized = openVolume * (price.Price - buyPrice);

                lines.Add($"{symbol.Base} | Real: {realized} UnReal: {unrealized} Total: {realized + unrealized}");

                totalPnL.Item1 += realized;
                totalPnL.Item2 += unrealized;
            }

            if (lines.Count > 1)
            {
                lines.Add($"ALL | Real: {totalPnL.Item1} UnReal: {totalPnL.Item2} Total: {totalPnL.Item1 + totalPnL.Item2}");
            }

            var userId = trades.First().UserId;

            _userManager.SendMessage(userId, string.Join("\r\n", lines));

            return Task.CompletedTask;
        }
    }
}