using MongoDB.Driver;

namespace BackBuddy.Api.Service.V1.Database.MongoDB
{
    public class MongoDBBuilder(IServiceCollection serviceCollection)
    {
        private readonly IServiceCollection _serviceCollection = serviceCollection;
        private string? _mongoDBConnection;
        private string? _mongoDBDatabaseName;

        public MongoDBBuilder AddConnection(string connection)
        {
            _mongoDBConnection = connection;
            return this;
        }

        public MongoDBBuilder AddDatabaseName(string databaseName)
        {
            _mongoDBDatabaseName = databaseName;
            return this;
        }

        public MongoDBBuilder AddCollection<TEntity>(string collectionName, Action<IMongoCollection<TEntity>>? callback = null)
        {
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentNullException(nameof(collectionName));
            _serviceCollection.AddSingleton(x =>
            {
                IMongoDatabase database = x.GetRequiredService<IMongoDatabase>();
                IMongoCollection<TEntity> collection = database.GetCollection<TEntity>(collectionName);
                callback?.Invoke(collection);
                return collection;
            });
            return this;
        }

        public MongoDBBuilder Connect()
        {
            if (string.IsNullOrEmpty(_mongoDBConnection))
                throw new InvalidDataException($"{nameof(_mongoDBConnection)} can not be null");
            if (string.IsNullOrEmpty(_mongoDBDatabaseName))
                throw new InvalidDataException($"{nameof(_mongoDBDatabaseName)} can not be null");

            _serviceCollection.AddSingleton<IMongoClient>(new MongoClient(_mongoDBConnection));

            _serviceCollection.AddSingleton(provider =>
            {
                var client = provider.GetRequiredService<IMongoClient>();
                return client.GetDatabase(_mongoDBDatabaseName);
            });

            return this;
        }
    }
}
