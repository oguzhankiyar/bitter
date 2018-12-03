using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public class AlertCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly ISymbolRepository _symbolRepository;
        private readonly IMessageService _messageService;
        private readonly ISocketServiceManager _socketServiceManager;

        public AlertCommand(IUserRepository userRepository,
                            IAlertRepository alertRepository,
                            ISymbolRepository symbolRepository,
                            IMessageService messageService,
                            ISocketServiceManager socketServiceManager)
        {
            _userRepository = userRepository;
            _alertRepository = alertRepository;
            _symbolRepository = symbolRepository;
            _messageService = messageService;
            _socketServiceManager = socketServiceManager;
        }

        public async Task ExecuteAsync(BotUpdateInput input)
        {
            string message = input.Message.Text.Trim();

            message = message.Replace((message + " ").Split(' ')[0], string.Empty).Trim();

            List<string> values = message.Contains(" ") ? message.Split(' ').ToList() : new List<string>() { message };

            UserEntity user = _userRepository.FindUser(input.Message.Chat.Id.ToString());

            if (user == null)
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Unauthorized!");

                return;
            }

            // "/alerts get <all|symbol>"
            if (values[0] == "get")
            {
                if (values.Count == 1)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                if (values[1] == "all")
                {
                    var alerts = _alertRepository.FindAlerts(user.Id);

                    List<string> lines = new List<string>();

                    foreach (var item in alerts)
                    {
                        var sym = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Id == item.SymbolId);

                        string line = $"{sym.FriendlyName} when";

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
                        await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "There are no alerts!");

                        return;
                    }

                    lines = lines.OrderBy(x => x).ToList();

                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), string.Join("\r\n", lines));

                    return;
                }
                else
                {
                    var symbol = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Name == values[1].ToUpperInvariant() || x.FriendlyName == values[1].ToUpperInvariant());

                    if (symbol == null)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, "Symbol is not found!");

                        return;
                    }

                    var alert = _alertRepository.FindAlert(user.Id, symbol.Id);

                    if (alert == null)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, "Alert is not found!");

                        return;
                    }

                    string result = $"{symbol.FriendlyName} when";

                    if (alert.LessValue.HasValue)
                    {
                        result += $" less than {alert.LessValue.Value}";
                    }

                    if (alert.GreaterValue.HasValue)
                    {
                        result += $" greater than {alert.GreaterValue.Value}";
                    }

                    await _messageService.SendMessageAsync(user.ChatId, result);
                }
            }
            // "/alerts set <symbol> <less|greater> <value>"
            else if (values[0] == "set")
            {
                if (values.Count < 4)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                var symbol = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Name == values[1].ToUpperInvariant() || x.FriendlyName == values[1].ToUpperInvariant());

                if (symbol == null)
                {
                    await _messageService.SendMessageAsync(user.ChatId, "Symbol is not found!");

                    return;
                }

                var alert = _alertRepository.FindAlert(user.Id, symbol.Id);

                if (values[2] == "less")
                {
                    decimal lessValue = Convert.ToDecimal(values[3]);

                    if (alert == null)
                    {
                        _alertRepository.InsertAlert(new AlertEntity()
                        {
                            UserId = user.Id,
                            SymbolId = symbol.Id,
                            LessValue = lessValue
                        });
                    }
                    else
                    {
                        alert.LessValue = lessValue;

                        _alertRepository.UpdateAlert(alert);
                    }

                    _socketServiceManager.UpdateAlert(user.Id);

                    await _messageService.SendMessageAsync(user.ChatId, "Success!");

                }
                else if (values[2] == "greater")
                {
                    decimal greaterValue = Convert.ToDecimal(values[3]);

                    if (alert == null)
                    {
                        _alertRepository.InsertAlert(new AlertEntity()
                        {
                            UserId = user.Id,
                            SymbolId = symbol.Id,
                            GreaterValue = greaterValue
                        });
                    }
                    else
                    {
                        alert.GreaterValue = greaterValue;

                        _alertRepository.UpdateAlert(alert);
                    }

                    _socketServiceManager.UpdateAlert(user.Id);

                    await _messageService.SendMessageAsync(user.ChatId, "Success!");
                }
                else
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }
            }
            // "/alerts del <all|symbol>"
            else if (values[0] == "del")
            {
                if (values.Count == 1)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                if (values[1] == "all")
                {
                    var alerts = _alertRepository.FindAlerts(user.Id);

                    foreach (var alert in alerts)
                    {
                        _alertRepository.RemoveAlert(alert.Id);
                    }

                    _socketServiceManager.UpdateAlert(user.Id);

                    await _messageService.SendMessageAsync(user.ChatId, "Success!");
                }
                else
                {
                    var symbol = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Name == values[1].ToUpperInvariant() || x.FriendlyName == values[1].ToUpperInvariant());

                    if (symbol == null)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, "Symbol is not found!");

                        return;
                    }

                    var alert = _alertRepository.FindAlert(user.Id, symbol.Id);

                    if (alert == null)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, "Alert is not found!");
                    }
                    else
                    {
                        _alertRepository.RemoveAlert(alert.Id);

                        _socketServiceManager.UpdateAlert(user.Id);

                        await _messageService.SendMessageAsync(user.ChatId, "Success!");
                    }
                }
            }
            else
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                return;
            }
        }
    }
}