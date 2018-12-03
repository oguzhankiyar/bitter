using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public class SubscriptionCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ISymbolRepository _symbolRepository;
        private readonly IMessageService _messageService;
        private readonly ISocketServiceManager _socketServiceManager;

        public SubscriptionCommand(IUserRepository userRepository,
                                   ISubscriptionRepository subscriptionRepository,
                                   ISymbolRepository symbolRepository,
                                   IMessageService messageService,
                                   ISocketServiceManager socketServiceManager)
        {
            _userRepository = userRepository;
            _subscriptionRepository = subscriptionRepository;
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

            // "/subscriptions get <all|symbol>"
            if (values[0] == "get")
            {
                if (values.Count == 1)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                if (values[1] == "all")
                {
                    var subscriptions = _subscriptionRepository.FindSubscriptions().Where(x => x.UserId == user.Id);

                    List<string> lines = new List<string>();

                    foreach (var item in subscriptions)
                    {
                        var sym = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Id == item.SymbolId);

                        lines.Add($"{sym.FriendlyName} for minimum {(item.MinimumChange * 100).ToString("0.00")}% change");
                    }

                    if (!lines.Any())
                    {
                        await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "There are no subscriptions!");

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

                    var subscription = _subscriptionRepository.FindSubscription(symbol.Id, user.Id);

                    if (subscription == null)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, "Subscription is not found!");

                        return;
                    }

                    await _messageService.SendMessageAsync(user.ChatId, $"{symbol.FriendlyName} for minimum {(subscription.MinimumChange * 100).ToString("0.00")}% change\r\n");
                }
            }
            // "/subscriptions set <all|symbol> <change?>"
            else if (values[0] == "set")
            {
                if (values.Count == 1)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                if (values[1] == "all")
                {
                    decimal minimumChange = (decimal)0.02;

                    if (values.Count > 2)
                    {
                        if (!decimal.TryParse(values[2], out minimumChange))
                        {
                            await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                            return;
                        }

                        minimumChange /= 100;
                    }

                    var symbols = _symbolRepository.FindSymbols();

                    foreach (var symbol in symbols)
                    {
                        var subscription = _subscriptionRepository.FindSubscription(symbol.Id, user.Id);

                        if (subscription == null)
                        {
                            _subscriptionRepository.InsertSubscription(new SubscriptionEntity()
                            {
                                UserId = user.Id,
                                SymbolId = symbol.Id,
                                MinimumChange = minimumChange
                            });
                        }
                        else
                        {
                            subscription.MinimumChange = minimumChange;

                            _subscriptionRepository.UpdateSubscription(subscription);
                        }
                    }
                }
                else
                {
                    decimal minimumChange = (decimal)0.02;

                    if (values.Count > 2)
                    {
                        if (!decimal.TryParse(values[2], out minimumChange))
                        {
                            await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                            return;
                        }

                        minimumChange /= 100;
                    }

                    var symbol = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Name == values[1].ToUpperInvariant() || x.FriendlyName == values[1].ToUpperInvariant());

                    if (symbol == null)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, "Symbol is not found!");

                        return;
                    }

                    if (minimumChange < symbol.MinimumChange)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, $"Minimum Change should be at least {(symbol.MinimumChange * 100).ToString("0.00")}!");

                        return;
                    }

                    var subscription = _subscriptionRepository.FindSubscription(symbol.Id, user.Id);

                    if (subscription == null)
                    {
                        _subscriptionRepository.InsertSubscription(new SubscriptionEntity()
                        {
                            UserId = user.Id,
                            SymbolId = symbol.Id,
                            MinimumChange = minimumChange
                        });
                    }
                    else
                    {
                        subscription.MinimumChange = minimumChange;

                        _subscriptionRepository.UpdateSubscription(subscription);
                    }
                }

                _socketServiceManager.UpdateSubscription(user.Id);

                await _messageService.SendMessageAsync(user.ChatId, "Success!");
            }
            // "/subscriptions del <all|symbol>"
            else if (values[0] == "del")
            {
                if (values.Count == 1)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                if (values[1] == "all")
                {
                    var subscriptions = _subscriptionRepository.FindSubscriptions().Where(x => x.UserId == user.Id);

                    foreach (var subscription in subscriptions)
                    {
                        _subscriptionRepository.RemoveSubscription(subscription.Id);
                    }
                }
                else
                {
                    var symbol = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Name == values[1].ToUpperInvariant() || x.FriendlyName == values[1].ToUpperInvariant());

                    if (symbol == null)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, "Symbol is not found!");

                        return;
                    }

                    var subscription = _subscriptionRepository.FindSubscription(symbol.Id, user.Id);

                    if (subscription == null)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, "Subscription is not found!");

                        return;
                    }

                    _subscriptionRepository.RemoveSubscription(subscription.Id);
                }

                _socketServiceManager.UpdateSubscription(user.Id);

                await _messageService.SendMessageAsync(user.ChatId, "Success!");
            }
            else
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                return;
            }
        }
    }
}