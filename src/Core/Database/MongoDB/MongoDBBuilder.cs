using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace BackBuddy.Core.Library.Database.MongoDB
{
    public class MongoDBBuilder(IServiceCollection serviceCollection, string connection, string databaseName)
    {
        private readonly IServiceCollection _serviceCollection = serviceCollection;
        private readonly string _mongoDBConnection = connection;
        private readonly string _mongoDBDatabaseName = databaseName;

        public MongoDBCollectionBuilder Connect()
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

            ConventionPack pack = [new GuidRepresentationConvention()];
            ConventionRegistry.Register("Guid Convention", pack, type => true);

            return new MongoDBCollectionBuilder(_serviceCollection);
        }
    }

    public class MongoDBCollectionBuilder(IServiceCollection serviceCollection)
    {
        public MongoDBCollectionBuilder AddCollection<TEntity>(string collectionName, Action<IMongoCollection<TEntity>>? collection = null)
        {
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentNullException(nameof(collectionName));
            serviceCollection.AddSingleton(x =>
            {
                IMongoDatabase database = x.GetRequiredService<IMongoDatabase>();
                IMongoCollection<TEntity> mongoCollection = database.GetCollection<TEntity>(collectionName);
                collection?.Invoke(mongoCollection);
                return mongoCollection;
            });
            return this;
        }
    }
}
