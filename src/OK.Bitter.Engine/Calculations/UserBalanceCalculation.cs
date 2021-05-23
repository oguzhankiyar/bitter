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
    public class UserBalanceCalculation
    {
        private readonly IStore<SymbolModel> _symbolStore;
        private readonly IStore<PriceModel> _priceStore;
        private readonly IUserManager _userManager;

        public UserBalanceCalculation(
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

            var totalBalance = decimal.Zero;
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


                var buyVolume = buyTrades.Sum(x => x.Volume);
                var sellVolume = sellTrades.Sum(x => x.Volume);

                var openVolume = buyVolume - sellVolume;
                var balance = openVolume * price.Price;

                if (balance != 0)
                {
                    lines.Add($"{symbol.Base} | Volume: {openVolume:0.00######} Balance: {balance:0.00######}");
                }

                totalBalance += balance;
            }

            if (lines.Count > 1)
            {
                lines.Add($"ALL | Balance: {totalBalance:0.00######}");
            }

            var userId = trades.First().UserId;

            _userManager.SendMessage(userId, string.Join("\r\n", lines));

            return Task.CompletedTask;
        }
    }
}