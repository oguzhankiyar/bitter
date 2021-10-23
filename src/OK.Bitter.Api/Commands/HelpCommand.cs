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

            if (User.Type == UserTypeEnum.Admin)
            {
                lines.Add("Send message to all users");
                lines.Add("- message <message>");
                lines.Add(string.Empty);

                lines.Add("Get symbol stream status");
                lines.Add("- status get <all|symbol>");
                lines.Add(string.Empty);
                
                lines.Add("Manage the users");
                lines.Add("- users get <all|username>");
                lines.Add("- users del <username>");
                lines.Add(string.Empty);
            }

            lines.Add("Manage the subscriptions");
            lines.Add("- subscriptions get <all|symbol>");
            lines.Add("- subscriptions set <all|symbol>");
            lines.Add("- subscriptions del <all|symbol>");
            lines.Add(string.Empty);

            lines.Add("Manage the alerts");
            lines.Add("- alerts get <all|symbol>");
            lines.Add("- alerts set <symbol> less <value>");
            lines.Add("- alerts set <symbol> greater <value>");
            lines.Add("- alerts del <symbol>");
            lines.Add(string.Empty);

            lines.Add("Manage the trades");
            lines.Add("- trades get <all|symbol>");
            lines.Add("- trades buy <symbol> <volume> <price> <time>");
            lines.Add("- trades sell <symbol> <volume> <price> <time>");
            lines.Add("- trades del <ticket>");
            lines.Add(string.Empty);

            lines.Add("Get price history");
            lines.Add("- prices get <all|symbol> <interval>");
            lines.Add(string.Empty);
            
            lines.Add("Get calculations");
            lines.Add("- calculations balance <all|symbol>");
            lines.Add("- calculations pnl <all|symbol>");
            lines.Add(string.Empty);

            lines.Add("Manage the settings");
            lines.Add("- settings get <all|key>");
            lines.Add("- settings set <key> <value>");
            lines.Add("- settings del <key>");
            lines.Add(string.Empty);

            lines.Add("Reset a symbol changes cache");
            lines.Add("- reset <all|symbol>");
            lines.Add(string.Empty);
            
            await ReplyAsync(string.Join("\r\n", lines));
        }
    }
}