using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public class PriceCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IPriceRepository _priceRepository;
        private readonly ISymbolRepository _symbolRepository;
        private readonly IMessageService _messageService;

        public PriceCommand(IUserRepository userRepository,
                            IPriceRepository priceRepository,
                            ISymbolRepository symbolRepository,
                            IMessageService messageService)
        {
            _userRepository = userRepository;
            _priceRepository = priceRepository;
            _symbolRepository = symbolRepository;
            _messageService = messageService;
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

            if (values[0] == "get")
            {
                if (values.Count < 3)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                DateTime startDate = DateTime.Now;

                if (values[2].EndsWith("h"))
                {
                    int interval;

                    if (!int.TryParse(values[2].Replace("h", string.Empty), out interval))
                    {
                        await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                        return;
                    }

                    startDate = DateTime.Now.AddHours(-1 * interval);
                }
                else if (values[2].EndsWith("m"))
                {
                    int interval;

                    if (!int.TryParse(values[2].Replace("m", string.Empty), out interval))
                    {
                        await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                        return;
                    }

                    startDate = DateTime.Now.AddMinutes(-1 * interval);
                }

                IEnumerable<PriceEntity> prices;

                if (values[1] == "all")
                {
                    prices = _priceRepository.FindPrices(startDate).OrderBy(x => x.Date);
                }
                else
                {
                    var symbol = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Name == values[1].ToUpperInvariant() || x.FriendlyName == values[1].ToUpperInvariant());

                    if (symbol == null)
                    {
                        await _messageService.SendMessageAsync(user.ChatId, "Symbol is not found!");

                        return;
                    }

                    prices = _priceRepository.FindPrices(symbol.Id, startDate).OrderBy(x => x.Date);
                }

                List<string> lines = new List<string>();

                foreach (var item in prices)
                {
                    var sym = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Id == item.SymbolId);

                    lines.Add($"{item.Date.AddHours(3).ToString("dd.MM.yyyy HH:mm:ss")} | {sym.FriendlyName}: {item.Price} {string.Format("[{0}%{1}]", (item.Change * 100).ToString("+0.00;-0.00;0"), GetTimeSpanString(DateTime.Now - item.LastChangeDate))}");
                }

                if (!lines.Any())
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "There are no prices!");

                    return;
                }

                lines = lines.OrderBy(x => x).ToList();

                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), string.Join("\r\n", lines));

                return;
            }
            else
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                return;
            }
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