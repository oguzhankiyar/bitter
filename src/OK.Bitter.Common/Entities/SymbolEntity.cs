namespace OK.Bitter.Common.Entities
{
    public class SymbolEntity : EntityBase
    {
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Base { get; set; }
        public string Quote { get; set; }
        public string Route { get; set; }
        public decimal MinimumChange { get; set; }
    }
}