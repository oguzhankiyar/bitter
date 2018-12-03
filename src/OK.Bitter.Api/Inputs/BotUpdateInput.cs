using Newtonsoft.Json;
using System;

namespace OK.Bitter.Api.Inputs
{
    public class BotUserInput
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }
    }

    public class BotChatInput
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }
    }

    public class BotMessageInput
    {
        [JsonProperty("message_id")]
        public int Id { get; set; }

        [JsonProperty("from")]
        public BotUserInput From { get; set; }
        
        [JsonProperty("chat")]
        public BotChatInput Chat { get; set; }
        
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class BotUpdateInput
    {
        [JsonProperty("update_id")]
        public int Id { get; set; }

        [JsonProperty("message")]
        public BotMessageInput Message { get; set; }
    }
}