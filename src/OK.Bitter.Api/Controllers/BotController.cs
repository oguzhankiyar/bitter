using Microsoft.AspNetCore.Mvc;
using OK.Bitter.Api.Commands;
using OK.Bitter.Api.Inputs;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Controllers
{
    [ApiController]
    [Route("api/bot")]
    public class BotController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IMessageService _messageService;

        public BotController(IUserRepository userRepository, IMessageRepository messageRepository, IMessageService messageService)
        {
            _userRepository = userRepository;
            _messageRepository = messageRepository;
            _messageService = messageService;
        }

        private static IDictionary<string, Type> commands = new Dictionary<string, Type>()
        {
            { "alerts", typeof(AlertCommand) },
            { "auth", typeof(AuthCommand) },
            { "help", typeof(HelpCommand) },
            { "message", typeof(MessageCommand) },
            { "prices", typeof(PriceCommand) },
            { "reset", typeof(ResetCommand) },
            { "settings", typeof(SettingCommand) },
            { "start", typeof(StartCommand) },
            { "status", typeof(StatusCommand) },
            { "subscriptions", typeof(SubscriptionCommand) },
            { "users", typeof(UserCommand) }
        };

        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> Update([FromBody] BotUpdateInput input)
        {
            UserEntity user = _userRepository.FindUser(input.Message.Chat.Id.ToString());

            _messageRepository.InsertMessage(new MessageEntity()
            {
                UserId = user?.Id,
                ChatId = input.Message.Chat.Id.ToString(),
                Text = input.Message.Text,
                Date = DateTime.Now
            });

            string commandString = (input.Message.Text + " ").Split(' ')[0].TrimStart('/');

            if (!commands.ContainsKey(commandString))
            {
                await _messageService.SendMessageAsync(input.Message.Chat.Id.ToString(), "Invalid command!");
            }
            else
            {
                Type commandType = commands[commandString];

                IBotCommand command = HttpContext.RequestServices.GetService(commandType) as IBotCommand;

                await command.ExecuteAsync(input);
            }

            return Ok();
        }
    }
}