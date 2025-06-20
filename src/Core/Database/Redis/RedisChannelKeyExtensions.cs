namespace BackBuddy.Core.Library.Database.Redis
{
    public static class RedisChannelKeyExtensions
    {
        public static string GetRedisChannelKey(this Type type)
        {
            return $"{RedisSubBackgroundService.ChannelPrefix}{type.FullName ?? type.Name}";
        }
    }
}
