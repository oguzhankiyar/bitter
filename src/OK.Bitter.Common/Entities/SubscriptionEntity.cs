using System;

namespace OK.Bitter.Common.Entities
{
    public class SubscriptionEntity : EntityBase
    {
        public string UserId { get; set; }

        public string SymbolId { get; set; }

        public decimal MinimumChange { get; set; }

        public decimal LastNotifiedPrice { get; set; }

        public DateTime LastNotifiedDate { get; set; }
    }
}