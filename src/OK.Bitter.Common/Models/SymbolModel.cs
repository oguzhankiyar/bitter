﻿using System.Collections.Generic;

namespace OK.Bitter.Common.Models
{
    public class SymbolModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Base { get; set; }
        public string Quote { get; set; }
        public List<SymbolRouteModel> Route { get; set; }
        public decimal MinimumChange { get; set; }
    }
}