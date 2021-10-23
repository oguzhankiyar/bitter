﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Repositories;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("trades")]
    public class TradeCommand : BaseCommand
    {
        private readonly ITradeRepository _tradeRepository;
        private readonly ISymbolRepository _symbolRepository;

        public TradeCommand(
            ITradeRepository tradeRepository,
            ISymbolRepository symbolRepository,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _tradeRepository = tradeRepository ?? throw new ArgumentNullException(nameof(tradeRepository));
            _symbolRepository = symbolRepository ?? throw new ArgumentNullException(nameof(symbolRepository));
        }

        [CommandCase("get", "{symbol}")]
        public async Task GetAsync(string symbol)
        {
            if (symbol == "all")
            {
                var trades = _tradeRepository.GetList(x => x.UserId == User.Id);

                var lines = new List<string>();

                foreach (var item in trades)
                {
                    var sym = _symbolRepository.Get(x => x.Id == item.SymbolId);

                    var line = $"{item.Time.ToString("dd.MM.yyyy HH:mm:ss")} | {sym.FriendlyName}: {item.Volume} x {item.Price} [{item.Type.ToString().ToUpperInvariant()} #{item.Ticket}]";

                    lines.Add(line);
                }

                if (!lines.Any())
                {
                    await ReplyAsync("There are no trades!");

                    return;
                }

                lines = lines.OrderBy(x => x).ToList();

                await ReplyPaginatedAsync(lines);
            }
            else
            {
                var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
                if (symbolEntity == null)
                {
                    await ReplyAsync("Symbol is not found!");

                    return;
                }

                var trades = _tradeRepository.GetList(x => x.UserId == User.Id && x.SymbolId == symbolEntity.Id);

                var lines = new List<string>();

                foreach (var item in trades)
                {
                    var sym = _symbolRepository.Get(x => x.Id == item.SymbolId);

                    var line = $"{item.Time.AddHours(3).ToString("dd.MM.yyyy HH:mm:ss")} | {sym.FriendlyName}: {item.Volume} x {item.Price} [{item.Type.ToString().ToUpperInvariant()} #{item.Ticket}]";

                    lines.Add(line);
                }

                if (!lines.Any())
                {
                    await ReplyAsync("There are no trades!");

                    return;
                }

                lines = lines.OrderBy(x => x).ToList();

                await ReplyPaginatedAsync(lines);
            }
        }

        [CommandCase("{type}", "{symbol}", "{volume}", "{price}", "{time}")]
        public async Task SetAsync(string type, string symbol, string volume, string price, string time)
        {
            if (!Enum.TryParse(type, true, out TradeTypeEnum typeValue))
            {
                await ReplyAsync("Invalid arguments!");

                return;
            }

            var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
            if (symbolEntity == null)
            {
                await ReplyAsync("Symbol is not found!");

                return;
            }

            if (!decimal.TryParse(volume, out decimal volumeValue))
            {
                await ReplyAsync("Invalid arguments!");

                return;
            }

            if (!decimal.TryParse(price, out decimal priceValue))
            {
                await ReplyAsync("Invalid arguments!");

                return;
            }

            if (!DateTime.TryParse(time, out DateTime timeValue))
            {
                await ReplyAsync("Invalid arguments!");

                return;
            }

            var userTrades = _tradeRepository.GetList(x => x.UserId == User.Id);

            if (typeValue == TradeTypeEnum.Sell)
            {
                var openVolume = userTrades.Where(x => x.SymbolId == symbolEntity.Id).Sum(x => (x.Type == TradeTypeEnum.Buy ? +1 : -1) * x.Volume);
                if (openVolume < volumeValue)
                {
                    await ReplyAsync("The volume should not be greater than open volume!");

                    return;
                }
            }

            var lastTicket = userTrades.Any() ? userTrades.Last().Ticket : 999;

            var trade = new TradeEntity
            {
                Ticket = lastTicket + 1,
                UserId = User.Id,
                SymbolId = symbolEntity.Id,
                Type = typeValue,
                Volume = volumeValue,
                Price = priceValue,
                Time = timeValue
            };

            _tradeRepository.Save(trade);

            await ReplyAsync("Success!");
        }

        [CommandCase("del", "{ticket}")]
        public async Task DelAsync(string ticket)
        {
            if (ticket == "all")
            {
                var trades = _tradeRepository.GetList(x => x.UserId == User.Id);

                foreach (var trade in trades)
                {
                    _tradeRepository.Delete(trade.Id);
                }

                await ReplyAsync("Success!");
            }
            else
            {
                if (!int.TryParse(ticket, out int ticketValue))
                {
                    await ReplyAsync("Invalid arguments!");

                    return;
                }

                var trade = _tradeRepository.Get(x => x.UserId == User.Id && x.Ticket == ticketValue);
                if (trade == null)
                {
                    await ReplyAsync("Trade is not found!");
                }
                else
                {
                    _tradeRepository.Delete(trade.Id);

                    await ReplyAsync("Success!");
                }
            }
        }
    }
}