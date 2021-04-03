using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("alerts")]
    public class AlertCommand : BaseCommand
    {
        private readonly IAlertRepository _alertRepository;
        private readonly ISymbolRepository _symbolRepository;
        private readonly ISocketServiceManager _socketServiceManager;

        public AlertCommand(
            IAlertRepository alertRepository,
            ISymbolRepository symbolRepository,
            ISocketServiceManager socketServiceManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _alertRepository = alertRepository ?? throw new ArgumentNullException(nameof(alertRepository));
            _symbolRepository = symbolRepository ?? throw new ArgumentNullException(nameof(symbolRepository));
            _socketServiceManager = socketServiceManager ?? throw new ArgumentNullException(nameof(socketServiceManager));
        }

        public override async Task OnPreExecutionAsync()
        {
            await base.OnPreExecutionAsync();

            if (User == null)
            {
                await ReplyAsync("Unauthorized!");

                await AbortAsync();
            }
        }

        [CommandCase("get", "{symbol}")]
        public async Task GetAsync(string symbol)
        {
            if (symbol == "all")
            {
                var alerts = _alertRepository.GetList(x => x.UserId == User.Id);

                var lines = new List<string>();

                foreach (var item in alerts)
                {
                    var sym = _symbolRepository.Get(x => x.Id == item.SymbolId);

                    var line = $"{sym.FriendlyName} when";

                    if (item.LessValue.HasValue)
                    {
                        line += $" less than {item.LessValue.Value}";
                    }

                    if (item.GreaterValue.HasValue)
                    {
                        line += $" greater than {item.GreaterValue.Value}";
                    }

                    lines.Add(line);
                }

                if (!lines.Any())
                {
                    await ReplyAsync("There are no alerts!");

                    return;
                }

                lines = lines.OrderBy(x => x).ToList();

                await ReplyAsync(string.Join("\r\n", lines));

                return;
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

                var result = $"{symbolEntity.FriendlyName} when";

                if (alert.LessValue.HasValue)
                {
                    result += $" less than {alert.LessValue.Value}";
                }

                if (alert.GreaterValue.HasValue)
                {
                    result += $" greater than {alert.GreaterValue.Value}";
                }

                await ReplyAsync(result);
            }
        }

        [CommandCase("set", "{symbol}", "{condition}", "{treshold}")]
        public async Task SetAsync(string symbol, string condition, string treshold)
        {
            var symbolEntity = _symbolRepository.Get(x => x.Name == symbol.ToUpperInvariant() || x.FriendlyName == symbol.ToUpperInvariant());
            if (symbolEntity == null)
            {
                await ReplyAsync("Symbol is not found!");

                return;
            }

            if (!decimal.TryParse(treshold, out decimal tresholdValue))
            {
                await ReplyAsync("Invalid arguments!");

                return;
            }

            var alert = _alertRepository.Get(x => x.UserId == User.Id && x.SymbolId == symbolEntity.Id);

            if (condition == "less")
            {
                if (alert == null)
                {
                    _alertRepository.Save(new AlertEntity()
                    {
                        UserId = User.Id,
                        SymbolId = symbolEntity.Id,
                        LessValue = tresholdValue
                    });
                }
                else
                {
                    alert.LessValue = tresholdValue;

                    _alertRepository.Save(alert);
                }

                _socketServiceManager.UpdateAlert(User.Id);

                await ReplyAsync("Success!");

            }
            else if (condition == "greater")
            {
                if (alert == null)
                {
                    _alertRepository.Save(new AlertEntity()
                    {
                        UserId = User.Id,
                        SymbolId = symbolEntity.Id,
                        GreaterValue = tresholdValue
                    });
                }
                else
                {
                    alert.GreaterValue = tresholdValue;

                    _alertRepository.Save(alert);
                }

                _socketServiceManager.UpdateAlert(User.Id);

                await ReplyAsync("Success!");
            }
            else
            {
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
                }

                _socketServiceManager.UpdateAlert(User.Id);

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

                    _socketServiceManager.UpdateAlert(User.Id);

                    await ReplyAsync("Success!");
                }
            }
        }
    }
}