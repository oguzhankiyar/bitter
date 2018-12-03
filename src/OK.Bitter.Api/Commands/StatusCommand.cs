using OK.Bitter.Api.HostedServices;
using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.Core.Managers;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public class StatusCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly ISymbolRepository _symbolRepository;
        private readonly IMessageService _messageService;
        private readonly ISocketHostedService _socketService;
        private readonly ISocketServiceManager _socketServiceManager;

        public StatusCommand(IUserRepository userRepository,
                             ISymbolRepository symbolRepository,
                             IMessageService messageService,
                             ISocketHostedService socketService,
                             ISocketServiceManager socketServiceManager)
        {
            _userRepository = userRepository;
            _symbolRepository = symbolRepository;
            _messageService = messageService;
            _socketService = socketService;
            _socketServiceManager = socketServiceManager;
        }

        public async Task ExecuteAsync(BotUpdateInput input)
        {
            string message = input.Message.Text.Trim();

            message = message.Replace((message + " ").Split(' ')[0], string.Empty).Trim();

            List<string> values = message.Contains(" ") ? message.Split(' ').ToList() : new List<string>() { message };

            UserEntity user = _userRepository.FindUser(input.Message.Chat.Id.ToString());

            if (user == null || user.Type != UserTypeEnum.Admin)
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Unauthorized!");

                return;
            }

            if (values[0] == "get")
            {
                if (values.Count == 1)
                {
                    await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                    return;
                }

                if (values[1] == "all")
                {
                    var response2 = _socketServiceManager.CheckStatus();

                    await _messageService.SendMessageAsync(user.ChatId, response2);

                    return;
                }

                var symbol = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Name == values[1].ToUpperInvariant() || x.FriendlyName == values[1].ToUpperInvariant());

                if (symbol == null)
                {
                    await _messageService.SendMessageAsync(user.ChatId, "Symbol is not found!");

                    return;
                }

                var response = _socketServiceManager.CheckSymbolStatus(symbol.Id);

                await _messageService.SendMessageAsync(user.ChatId, response);
            }
            else
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid arguments!");

                return;
            }
        }
    }
}