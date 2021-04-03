using System;
using MongoDB.Driver;
using OK.Bitter.Common.Entities;

namespace OK.Bitter.DataAccess.DataContexts
{
    public class BitterDataContext : IDisposable
    {
        public IMongoClient Client { get; }
        public IMongoDatabase Database { get; }

        public IMongoCollection<AlertEntity> Alerts => GetCollection<AlertEntity>(nameof(Alerts));
        public IMongoCollection<MessageEntity> Messages => GetCollection<MessageEntity>(nameof(Messages));
        public IMongoCollection<PriceEntity> Prices => GetCollection<PriceEntity>(nameof(Prices));
        public IMongoCollection<SettingEntity> Settings => GetCollection<SettingEntity>(nameof(Settings));
        public IMongoCollection<SubscriptionEntity> Subscriptions => GetCollection<SubscriptionEntity>(nameof(Subscriptions));
        public IMongoCollection<SymbolEntity> Symbols => GetCollection<SymbolEntity>(nameof(Symbols));
        public IMongoCollection<UserEntity> Users => GetCollection<UserEntity>(nameof(Users));

        public BitterDataContext(string connectionString, string databaseName)
        {
            Client = new MongoClient(connectionString);
            Database = Client.GetDatabase(databaseName);
        }

        public IMongoCollection<TEntity> GetCollection<TEntity>(string collectionName)
        {
            return Database.GetCollection<TEntity>(collectionName);
        }

        public void Dispose()
        {

        }
    }
}