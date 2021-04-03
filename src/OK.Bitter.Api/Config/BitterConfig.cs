using System.Collections.Generic;
using OK.Bitter.Common.Enumerations;
using OK.Bitter.DataAccess.Config;
using OK.Bitter.Services.Call.IFTTT;
using OK.Bitter.Services.Message.Telegram;

namespace OK.Bitter.Api.Config
{
    public class BitterConfig
    {
        public string HookPath { get; set; }
        public IDictionary<UserTypeEnum, string> Passwords { get; set; }
        public BitterDataAccessConfig DataAccess { get; set; }
        public IFTTTCallServiceConfig IFTTT { get; set; }
        public TelegramMessageServiceConfig Telegram { get; set; }
    }
}