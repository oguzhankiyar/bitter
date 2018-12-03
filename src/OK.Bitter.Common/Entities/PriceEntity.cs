using System;

namespace OK.Bitter.Common.Entities
{
    public class PriceEntity : EntityBase
    {
        public string SymbolId { get; set; }

        public decimal Price { get; set; }

        public decimal Change { get; set; }

        public DateTime LastChangeDate { get; set; }

        public DateTime Date { get; set; }
    }
}