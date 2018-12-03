using System;

namespace OK.Bitter.Common.Entities
{
    public class EntityBase
    {
        public string Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }
    }
}