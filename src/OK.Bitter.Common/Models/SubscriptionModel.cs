using System;

namespace OK.Bitter.Common.Models
{
    public class SubscriptionModel
    {
        public string UserId { get; set; }

        public string SymbolId { get; set; }

        public decimal MinimumChange { get; set; }

        public decimal LastNotifiedPrice { get; set; }

        public DateTime LastNotifiedDate { get; set; }
    }
}