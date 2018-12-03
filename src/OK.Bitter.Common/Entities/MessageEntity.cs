using System;

namespace OK.Bitter.Common.Entities
{
    public class MessageEntity : EntityBase
    {
        public string UserId { get; set; }

        public string ChatId { get; set; }

        public string Text { get; set; }

        public DateTime Date { get; set; }
    }
}