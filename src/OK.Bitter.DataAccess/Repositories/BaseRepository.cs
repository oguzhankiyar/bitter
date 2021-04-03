using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using OK.Bitter.Common.Entities;
using OK.Bitter.Core.Repositories;

namespace OK.Bitter.DataAccess.Repositories
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : EntityBase
    {
        private readonly IMongoCollection<TEntity> _collection;

        public BaseRepository(IMongoCollection<TEntity> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public IEnumerable<TEntity> GetList()
        {
            return _collection.Find(new JsonFilterDefinition<TEntity>("{}")).ToList();
        }

        public IEnumerable<TEntity> GetList(Expression<Func<TEntity, bool>> predicate)
        {
            return _collection.Find(predicate).ToList();
        }

        public TEntity Get(Expression<Func<TEntity, bool>> predicate)
        {
            return _collection.Find(predicate).FirstOrDefault();
        }

        public TEntity GetById(string id)
        {
            return _collection.Find(x => x.Id.Equals(id)).FirstOrDefault();
        }

        public TEntity Save(TEntity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                entity.Id = ObjectId.GenerateNewId().ToString();
                entity.CreatedDate = DateTime.UtcNow;
            }

            entity.UpdatedDate = DateTime.UtcNow;

            _collection.ReplaceOne(
                   x => x.Id.Equals(entity.Id),
                   entity,
                   new UpdateOptions
                   {
                       IsUpsert = true
                   });

            return entity;
        }

        public void Delete(string id)
        {
            _collection.DeleteOne(x => x.Id.Equals(id));
        }
    }
}