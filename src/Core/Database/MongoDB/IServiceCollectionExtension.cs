using Microsoft.Extensions.DependencyInjection;

namespace BackBuddy.Api.Service.V1.Database.MongoDB
{
    public static class IServiceCollectionExtension
    {
        public static MongoDBBuilder AddMongoDB(this IServiceCollection collection, string connectionString, string databaseName)
        {
            return new MongoDBBuilder(collection, connectionString, databaseName);
        }
    }
}
