namespace BackBuddy.Api.Service.V1.Database.Redis
{
    public static class TypeExtension
    {
        public static string GetRedisChannelKey(this Type type)
        {
            return $"{RedisSubBackgroundService.ChannelPrefix}{type.FullName ?? type.Name}";
        }
    }
}
