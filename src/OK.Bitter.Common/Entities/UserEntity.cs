using OK.Bitter.Common.Enumerations;

namespace OK.Bitter.Common.Entities
{
    public class UserEntity : EntityBase
    {
        public string Username { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string ChatId { get; set; }

        public UserTypeEnum Type { get; set; }
    }
}