using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;

namespace OK.Bitter.DataAccess.Repositories
{
    public class UserRepository : BaseRepository<UserEntity>, IUserRepository
    {
        public UserRepository(BitterDataContext context) : base(context.Users)
        {

        }
    }
}