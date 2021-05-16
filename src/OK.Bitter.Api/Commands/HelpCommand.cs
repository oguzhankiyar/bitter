using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OK.Bitter.Common.Enumerations;
using OK.GramHook;

namespace OK.Bitter.Api.Commands
{
    [Command("help")]
    public class HelpCommand : BaseCommand
    {
        public HelpCommand(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        [CommandCase]
        public async Task HelpAsync()
        {
            var lines = new List<string>();

            if (User == null)
            {
                lines.Add("Start the bot");
                lines.Add("- start");
                lines.Add(string.Empty);
            }
            else
            {
                if (User.Type == UserTypeEnum.Admin)
                {
                    lines.Add("Send message to all users");
                    lines.Add("- message <message>");
                    lines.Add(string.Empty);

                    lines.Add("Get symbol stream status");
                    lines.Add("- status get <all|symbol>");
                    lines.Add(string.Empty);
                }

                lines.Add("Manage your subscriptions");
                lines.Add("- subscriptions get <all|symbol>");
                lines.Add("- subscriptions set <all|symbol>");
                lines.Add("- subscriptions del <all|symbol>");
                lines.Add(string.Empty);

                lines.Add("Manage your alerts");
                lines.Add("- alerts get <all|symbol>");
                lines.Add("- alerts set <symbol> less <value>");
                lines.Add("- alerts set <symbol> greater <value>");
                lines.Add("- alerts del <symbol>");
                lines.Add(string.Empty);

                lines.Add("Manage your trades");
                lines.Add("- trades get <all|symbol>");
                lines.Add("- trades buy <symbol> <volume> <price> <time>");
                lines.Add("- trades sell <symbol> <volume> <price> <time>");
                lines.Add("- trades del <ticket>");
                lines.Add(string.Empty);

                lines.Add("Get price history");
                lines.Add("- prices get <all|symbol> <interval>");
                lines.Add(string.Empty);

                lines.Add("Manage your settings");
                lines.Add("- settings get <all|key>");
                lines.Add("- settings set <key> <value>");
                lines.Add("- settings del <key>");
                lines.Add(string.Empty);

                lines.Add("Reset a symbol changes cache");
                lines.Add("- reset <all|symbol>");
                lines.Add(string.Empty);
            }

            await ReplyAsync(string.Join("\r\n", lines));
        }
    }
}