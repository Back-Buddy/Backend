namespace BackBuddy.Api.Service.V1.Database.MongoDB
{
    public record MongoDBConnectionConfig
    {
        public required string Connection { get; init; }
        public required string DatabaseName { get; init; }
    }
}
