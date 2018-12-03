using OK.Bitter.Common.Models;
using System.Collections.Generic;

namespace OK.Bitter.Core.Managers
{
    public interface IUserManager
    {
        List<UserModel> GetUsers();

        bool SendMessage(string userId, string message);

        bool CallUser(string userId, string message);
    }
}