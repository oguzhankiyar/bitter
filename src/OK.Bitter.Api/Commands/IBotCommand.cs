using OK.Bitter.Api.Inputs;
using System.Threading.Tasks;

namespace OK.Bitter.Api.Commands
{
    public interface IBotCommand
    {
        Task ExecuteAsync(BotUpdateInput input);
    }
}