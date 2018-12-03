using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;
using OK.Bitter.DataAccess.DataContexts;
using System.Collections.Generic;

namespace OK.Bitter.DataAccess.Repositories
{
    public class UserRepository : BaseRepository<UserEntity>, IUserRepository
    {
        public UserRepository(BitterDataContext context) : base(context, "Users")
        {
        }

        public IEnumerable<UserEntity> FindUsers()
        {
            return GetList();
        }

        public UserEntity FindUser(string chatId)
        {
            return Get(x => x.ChatId == chatId);
        }

        public UserEntity FindUserById(string id)
        {
            return GetById(id);
        }

        public UserEntity InsertUser(UserEntity user)
        {
            return Save(user);
        }


        public bool UpdateUser(UserEntity user)
        {
            Save(user);

            return true;
        }
    }
}