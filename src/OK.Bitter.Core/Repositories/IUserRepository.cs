using OK.Bitter.Common.Entities;
using System.Collections.Generic;

namespace OK.Bitter.Core.Repositories
{
    public interface IUserRepository
    {
        IEnumerable<UserEntity> FindUsers();

        UserEntity FindUser(string username);

        UserEntity FindUserById(string id);

        UserEntity InsertUser(UserEntity user);

        bool UpdateUser(UserEntity user);
    }
}