namespace OK.Bitter.Common.Entities
{
    public class SymbolEntity : EntityBase
    {
        public string Name { get; set; }

        public string FriendlyName { get; set; }

        public decimal MinimumChange { get; set; }
    }
}