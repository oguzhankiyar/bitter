using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Engine.Extensions;
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
                prices = _priceRepository.GetList(x => x.CreatedDate >= startDate).OrderBy(x => x.Date);
            }
            else
            {
                var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
                if (symbolEntity == null)
                {
                    await ReplyAsync("Symbol is not found!");

                    return;
                }

                prices = _priceRepository.GetList(x => x.SymbolId == symbolEntity.Id && x.Date >= startDate).OrderBy(x => x.Date);
            }

            var lines = new List<string>();

            foreach (var item in prices)
            {
                var sym = _symbolRepository.Get(x => x.Id == item.SymbolId);

                lines.Add($"{item.Date:dd.MM.yyyy HH:mm:ss} | {sym.Base}: {item.Price:0.00######} {sym.Quote} {string.Format("[{0}% {1}]", (item.Change * 100).ToString("+0.00;-0.00;0"), (DateTime.UtcNow - item.LastChangeDate).ToIntervalString())}");
            }

            if (!lines.Any())
            {
                await ReplyAsync("There are no prices!");

                return;
            }

            lines = lines.OrderBy(x => x).ToList();

            var skip = 0;
            var take = 25;

            while (skip < lines.Count)
            {
                var items = lines.Skip(skip).Take(take);
                await ReplyAsync(string.Join("\r\n", items));
                await Task.Delay(500);

                skip += take;
            }
        }
    }
}