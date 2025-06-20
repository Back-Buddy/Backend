namespace BackBuddy.Core.Library.Database.Redis
{
    public record RedisConnectionConfig
    {
        public required string Connection { get; init; }
        public required string DatabaseName { get; init; }
    }
}
