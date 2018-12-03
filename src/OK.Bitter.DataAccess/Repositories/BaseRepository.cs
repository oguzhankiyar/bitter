using MongoDB.Bson;
using MongoDB.Driver;
using OK.Bitter.Common.Entities;
using OK.Bitter.DataAccess.DataContexts;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace OK.Bitter.DataAccess.Repositories
{
    public class BaseRepository<TEntity> where TEntity : EntityBase
    {
        private IMongoCollection<TEntity> collection;

        public BaseRepository(BitterDataContext context, string collectionName)
        {
            collection = context.Database.GetCollection<TEntity>(collectionName);
        }

        public IEnumerable<TEntity> GetList()
        {
            return collection.Find(new JsonFilterDefinition<TEntity>("{}")).ToList();
        }

        public IEnumerable<TEntity> GetList(Expression<Func<TEntity, bool>> predicate)
        {
            return collection.Find(predicate).ToList();
        }

        public TEntity Get(Expression<Func<TEntity, bool>> predicate)
        {
            return collection.Find(predicate).FirstOrDefault();
        }

        public TEntity GetById(string id)
        {
            return collection.Find(x => x.Id.Equals(id)).FirstOrDefault();
        }

        public TEntity Save(TEntity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                entity.Id = ObjectId.GenerateNewId().ToString();
                entity.CreatedDate = DateTime.Now;
            }

            entity.UpdatedDate = DateTime.Now;

            collection.ReplaceOne(
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
            collection.DeleteOne(x => x.Id.Equals(id));
        }
    }
}