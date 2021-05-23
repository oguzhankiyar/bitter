using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using OK.Bitter.Common.Models;
using OK.Bitter.Core.Managers;

namespace OK.Bitter.Engine.Stores
{
    public class UserStore : IStore<UserModel>
    {
        public event EventHandler<UserModel> OnInserted;
        public event EventHandler<UserModel> OnUpdated;
        public event EventHandler<UserModel> OnDeleted;

        private readonly IUserManager _userManager;

        private ConcurrentBag<UserModel> _items = new ConcurrentBag<UserModel>();

        public UserStore(IUserManager userManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public void Init()
        {
            _userManager
                .GetUsers()
                .ForEach(Upsert);
        }

        public List<UserModel> Get(Func<UserModel, bool> expression = null)
        {
            if (expression == null)
            {
                return _items.ToList();
            }

            return _items.Where(expression).ToList();
        }

        public UserModel Find(Func<UserModel, bool> expression)
        {
            return _items.FirstOrDefault(expression);
        }

        public void Upsert(UserModel user)
        {
            var item = _items.FirstOrDefault(x => x.Id == user.Id);
            if (item != null)
            {
                item.ChatId = user.ChatId;
                OnUpdated?.Invoke(this, user);
            }
            else
            {
                _items.Add(user);
                OnInserted?.Invoke(this, user);
            }
        }

        public void Delete(UserModel user)
        {
            var items = _items.ToList();
            items.RemoveAll(x => x.Id == user.Id);
            _items = new ConcurrentBag<UserModel>(items);

            OnDeleted?.Invoke(this, user);
        }

        public void Delete(Func<UserModel, bool> filter = null)
        {
            var users = Get(filter);

            foreach (var user in users)
            {
                var items = _items.ToList();
                items.RemoveAll(x => x.Id == user.Id);
                _items = new ConcurrentBag<UserModel>(items);

                OnDeleted?.Invoke(this, user);
            }
        }
    }
}