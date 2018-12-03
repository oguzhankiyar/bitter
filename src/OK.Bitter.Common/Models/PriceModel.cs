using System;

namespace OK.Bitter.Common.Models
{
    public class PriceModel
    {
        public string SymbolId { get; set; }

        public decimal Price { get; set; }

        public DateTime Date { get; set; }
    }
}