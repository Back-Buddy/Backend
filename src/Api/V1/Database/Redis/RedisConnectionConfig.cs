namespace BackBuddy.Api.Service.V1.Database.Redis
{
    public record RedisConnectionConfig
    {
        public required string Connection { get; init; }
        public required string DatabaseName { get; init; }
    }
}
