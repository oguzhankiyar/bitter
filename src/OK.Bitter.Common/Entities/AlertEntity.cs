using System;

namespace OK.Bitter.Common.Entities
{
    public class AlertEntity : EntityBase
    {
        public string UserId { get; set; }

        public string SymbolId { get; set; }

        public decimal? LessValue { get; set; }

        public decimal? GreaterValue { get; set; }

        public DateTime? LastAlertDate { get; set; }
    }
}
