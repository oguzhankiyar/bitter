using System;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Engine.Calculations;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("calculations")]
    public class CalculationCommand : BaseCommand
    {
        private readonly ITradeRepository _tradeRepository;
        private readonly ISymbolRepository _symbolRepository;
        private readonly UserPnLCalculation _userPnLCalculation;

        public CalculationCommand(
            ITradeRepository tradeRepository,
            ISymbolRepository symbolRepository,
            UserPnLCalculation userPnLCalculation,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _tradeRepository = tradeRepository ?? throw new System.ArgumentNullException(nameof(tradeRepository));
            _symbolRepository = symbolRepository ?? throw new System.ArgumentNullException(nameof(symbolRepository));
            _userPnLCalculation = userPnLCalculation ?? throw new System.ArgumentNullException(nameof(userPnLCalculation)); ;
        }

        [CommandCase("pnl", "{symbol}")]
        public async Task PnLAsync(string symbol)
        {
            var trades = _tradeRepository.GetList(x => x.UserId == User.Id);

            if (symbol != "all")
            {
                var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
                if (symbolEntity == null)
                {
                    await ReplyAsync("Symbol is not found!");

                    return;
                }

                trades = trades.Where(x => x.SymbolId == symbolEntity.Id);
            }

            if (!trades.Any())
            {
                await ReplyAsync("There are no trades!");

                return;
            }

            await _userPnLCalculation.CalculateAsync(trades.Select(x => new TradeModel
            {
                Ticket = x.Ticket,
                UserId = x.UserId,
                SymbolId = x.SymbolId,
                Type = x.Type,
                Volume = x.Volume,
                Price = x.Price,
                Time = x.Time
            }).ToList());
        }
    }
}