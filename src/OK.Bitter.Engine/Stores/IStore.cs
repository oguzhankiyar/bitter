using System;
using System.Collections.Generic;

namespace OK.Bitter.Engine.Stores
{
    public interface IStore<T>
    {
        event EventHandler<T> OnInserted;
        event EventHandler<T> OnUpdated;
        event EventHandler<T> OnDeleted;

        void Init();
        List<T> Get(Func<T, bool> filter = null);
        T Find(Func<T, bool> filter);
        void Upsert(T user);
        void Delete(T user);
        void Delete(Func<T, bool> filter = null);
    }
}