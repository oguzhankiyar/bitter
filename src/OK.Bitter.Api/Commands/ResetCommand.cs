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
    public class ResetCommand : IBotCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly ISymbolRepository _symbolRepository;
        private readonly IMessageService _messageService;
        private readonly ISocketServiceManager _socketServiceManager;

        public ResetCommand(IUserRepository userRepository,
                            ISymbolRepository symbolRepository,
                            IMessageService messageService,
                            ISocketServiceManager socketServiceManager)
        {
            _userRepository = userRepository;
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

            if (values[0] == "all")
            {
                _socketServiceManager.ResetCache(user.Id);

                await _messageService.SendMessageAsync(user.ChatId, "Success!");

                return;
            }
            else
            {
                var symbol = _symbolRepository.FindSymbols().FirstOrDefault(x => x.Name == values[0].ToUpperInvariant() || x.FriendlyName == values[0].ToUpperInvariant());

                if (symbol == null)
                {
                    await _messageService.SendMessageAsync(user.ChatId, "Symbol is not found!");

                    return;
                }

                _socketServiceManager.ResetCache(user.Id, symbol.Id);

                await _messageService.SendMessageAsync(user.ChatId, "Success!");
            }
        }
    }
}