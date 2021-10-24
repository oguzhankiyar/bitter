using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Engine.Stores;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("alerts")]
    public class AlertCommand : BaseCommand
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IStore<AlertModel> _alertStore;
        private readonly ISymbolRepository _symbolRepository;

        public AlertCommand(
            IAlertRepository alertRepository,
            IStore<AlertModel> alertStore,
            ISymbolRepository symbolRepository,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _alertRepository = alertRepository ?? throw new ArgumentNullException(nameof(alertRepository));
            _alertStore = alertStore ?? throw new ArgumentNullException(nameof(alertStore));
            _symbolRepository = symbolRepository ?? throw new ArgumentNullException(nameof(symbolRepository));
        }

        [CommandCase("get", "{symbol}")]
        public async Task GetAsync(string symbol)
        {
            if (symbol == "all")
            {
                var alerts = _alertRepository.GetList(x => x.UserId == User.Id);
                if (!alerts.Any())
                {
                    await ReplyAsync("There are no alerts!");

                    return;
                }

                var lines = new List<string>();

                foreach (var alert in alerts)
                {
                    var symbolEntity = _symbolRepository.Get(x => x.Id == alert.SymbolId);

                    lines.Add(FormatAlert(symbolEntity, alert));
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

                var alert = _alertRepository.Get(x => x.UserId == User.Id && x.SymbolId == symbolEntity.Id);
                if (alert == null)
                {
                    await ReplyAsync("Alert is not found!");

                    return;
                }

                await ReplyAsync(FormatAlert(symbolEntity, alert));
            }
        }

        [CommandCase("set", "{symbol}", "{condition}", "{threshold}")]
        public async Task SetAsync(string symbol, string condition, string threshold)
        {
            var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
            if (symbolEntity == null)
            {
                await ReplyAsync("Symbol is not found!");

                return;
            }

            if (!decimal.TryParse(threshold, out var thresholdValue))
            {
                await ReplyAsync("Invalid arguments!");

                return;
            }

            var alert = _alertRepository.Get(x => x.UserId == User.Id && x.SymbolId == symbolEntity.Id);

            switch (condition)
            {
                case "less":
                {
                    if (alert == null)
                    {
                        alert = new AlertEntity()
                        {
                            UserId = User.Id,
                            SymbolId = symbolEntity.Id,
                            LessValue = thresholdValue
                        };
                    }
                    else
                    {
                        alert.LessValue = thresholdValue;
                    }

                    _alertRepository.Save(alert);
                    _alertStore.Upsert(new AlertModel
                    {
                        UserId = alert.UserId,
                        SymbolId = alert.SymbolId,
                        LessValue = alert.LessValue,
                        GreaterValue = alert.GreaterValue,
                        LastAlertDate = alert.LastAlertDate
                    });

                    await ReplyAsync("Success!");
                    
                    break;
                }
                case "greater":
                {
                    if (alert == null)
                    {
                        alert = new AlertEntity()
                        {
                            UserId = User.Id,
                            SymbolId = symbolEntity.Id,
                            GreaterValue = thresholdValue
                        };
                    }
                    else
                    {
                        alert.GreaterValue = thresholdValue;
                    }

                    _alertRepository.Save(alert);
                    _alertStore.Upsert(new AlertModel
                    {
                        UserId = alert.UserId,
                        SymbolId = alert.SymbolId,
                        LessValue = alert.LessValue,
                        GreaterValue = alert.GreaterValue,
                        LastAlertDate = alert.LastAlertDate
                    });

                    await ReplyAsync("Success!");
                    
                    break;
                }
                default:
                    await ReplyAsync("Invalid arguments!");

                    return;
            }
        }

        [CommandCase("del", "{symbol}")]
        public async Task DelAsync(string symbol)
        {
            if (symbol == "all")
            {
                var alerts = _alertRepository.GetList(x => x.UserId == User.Id);

                foreach (var alert in alerts)
                {
                    _alertRepository.Delete(alert.Id);
                    _alertStore.Delete(new AlertModel
                    {
                        UserId = alert.UserId,
                        SymbolId = alert.SymbolId,
                        LessValue = alert.LessValue,
                        GreaterValue = alert.GreaterValue,
                        LastAlertDate = alert.LastAlertDate
                    });
                }

                await ReplyAsync("Success!");
            }
            else
            {
                var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
                if (symbolEntity == null)
                {
                    await ReplyAsync("Symbol is not found!");

                    return;
                }

                var alert = _alertRepository.Get(x => x.UserId == User.Id && x.SymbolId == symbolEntity.Id);
                if (alert == null)
                {
                    await ReplyAsync("Alert is not found!");
                }
                else
                {
                    _alertRepository.Delete(alert.Id);
                    _alertStore.Delete(new AlertModel
                    {
                        UserId = alert.UserId,
                        SymbolId = alert.SymbolId,
                        LessValue = alert.LessValue,
                        GreaterValue = alert.GreaterValue,
                        LastAlertDate = alert.LastAlertDate
                    });

                    await ReplyAsync("Success!");
                }
            }
        }

        private string FormatAlert(SymbolEntity symbol, AlertEntity alert)
        {
            var result = $"{symbol.Base} when";

            if (alert.LessValue.HasValue)
            {
                result += $" less than {alert.LessValue.Value} {symbol.Quote}";
            }

            if (alert.GreaterValue.HasValue)
            {
                result += $" greater than {alert.GreaterValue.Value} {symbol.Quote}";
            }

            return result;
        }
    }
}