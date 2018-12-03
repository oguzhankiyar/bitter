using MongoDB.Driver;
using System;

namespace OK.Bitter.DataAccess.DataContexts
{
    public class BitterDataContext : IDisposable
    {
        public IMongoClient Client;
        public IMongoDatabase Database;

        public BitterDataContext(string serverName, string databaseName)
        {
            Client = new MongoClient(serverName);
            Database = Client.GetDatabase(databaseName);
        }

        public void Dispose()
        {

        }
    }
}