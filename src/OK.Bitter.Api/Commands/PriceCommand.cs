using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("prices")]
    public class PriceCommand : BaseCommand
    {
        private readonly IPriceRepository _priceRepository;
        private readonly ISymbolRepository _symbolRepository;

        public PriceCommand(
            IPriceRepository priceRepository,
            ISymbolRepository symbolRepository,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _priceRepository = priceRepository ?? throw new ArgumentNullException(nameof(priceRepository));
            _symbolRepository = symbolRepository ?? throw new ArgumentNullException(nameof(symbolRepository));
        }

        [CommandCase("get", "{symbol}", "{interval}")]
        public async Task GetAsync(string symbol, string interval)
        {
            if (User == null)
            {
                await ReplyAsync("Unauthorized!");

                return;
            }

            var startDate = DateTime.UtcNow;

            if (interval.EndsWith("h"))
            {
                if (!int.TryParse(interval.Replace("h", string.Empty), out int intervalValue))
                {
                    await ReplyAsync("Invalid arguments!");

                    return;
                }

                startDate = DateTime.UtcNow.AddHours(-1 * intervalValue);
            }
            else if (interval.EndsWith("m"))
            {
                if (!int.TryParse(interval.Replace("m", string.Empty), out int intervalValue))
                {
                    await ReplyAsync("Invalid arguments!");

                    return;
                }

                startDate = DateTime.UtcNow.AddMinutes(-1 * intervalValue);
            }

            var prices = Enumerable.Empty<PriceEntity>();

            if (symbol == "all")
            {
                prices = _priceRepository.FindPrices(startDate).OrderBy(x => x.Date);
            }
            else
            {
                var symbolEntity = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
                if (symbolEntity == null)
                {
                    await ReplyAsync("Symbol is not found!");

                    return;
                }

                prices = _priceRepository.FindPrices(symbolEntity.Id, startDate).OrderBy(x => x.Date);
            }

            var lines = new List<string>();

            foreach (var item in prices)
            {
                var sym = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Id == item.SymbolId);

                lines.Add($"{item.Date.AddHours(3).ToString("dd.MM.yyyy HH:mm:ss")} | {sym.FriendlyName}: {item.Price} {string.Format("[{0}%{1}]", (item.Change * 100).ToString("+0.00;-0.00;0"), GetTimeSpanString(DateTime.Now - item.LastChangeDate))}");
            }

            if (!lines.Any())
            {
                await ReplyAsync("There are no prices!");

                return;
            }

            lines = lines.OrderBy(x => x).ToList();

            await ReplyAsync(string.Join("\r\n", lines));
        }

        private static string GetTimeSpanString(TimeSpan span)
        {
            if (span.Days > 0)
            {
                return " in " + span.Days + " day(s)";
            }
            else if (span.Hours > 0)
            {
                return " in " + span.Hours + " hour(s)";
            }
            else if (span.Minutes > 0)
            {
                return " in " + span.Minutes + " minute(s)";
            }
            else if (span.Seconds > 0)
            {
                return " in " + span.Seconds + " second(s)";
            }
            else if (span.Milliseconds > 0)
            {
                return " in " + span.Milliseconds + " millisecond(s)";
            }

            return string.Empty;
        }
    }
}