﻿using System;
using OK.Bitter.Common.Enumerations;

namespace OK.Bitter.Common.Entities
{
    public class TradeEntity : EntityBase
    {
        public int Ticket { get; set; }

        public string UserId { get; set; }

        public string SymbolId { get; set; }

        public TradeTypeEnum Type { get; set; }

        public decimal Volume { get; set; }

        public decimal Price { get; set; }

        public DateTime Time { get; set; }
    }
}