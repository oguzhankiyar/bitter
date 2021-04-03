using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using OK.Bitter.Common.Entities;

namespace OK.Bitter.Core.Repositories
{
    public interface IBaseRepository<TEntity> where TEntity : EntityBase
    {
        IEnumerable<TEntity> GetList();
        IEnumerable<TEntity> GetList(Expression<Func<TEntity, bool>> predicate);
        TEntity Get(Expression<Func<TEntity, bool>> predicate);
        TEntity GetById(string id);
        TEntity Save(TEntity entity);
        void Delete(string id);
    }
}