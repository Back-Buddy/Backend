namespace BackBuddy.Api.Service.V1.Database.MongoDB
{
    public static class IServiceCollectionExtension
    {
        public static MongoDBBuilder AddMongoDB(this IServiceCollection collection)
        {
            return new MongoDBBuilder(collection);
        }
    }
}
