namespace BackBuddy.Api.Service.V1.Database.Redis
{
    public static class RedisSubBuilderExtension
    {
        public static RedisSubBuilder AddRedisSub(this IServiceCollection services)
        {
            services.AddSingleton<RedisSubBackgroundService>();
            return new RedisSubBuilder(services);
        }
    }

    public class RedisSubBuilder(IServiceCollection services)
    {
        private readonly IServiceCollection _services = services;

        private readonly HashSet<(Type ConsumerType, Type MessageType)> _consumers = [];

        public RedisSubBuilder AddConsumer<TConsumer, TMessage>() where TConsumer : IConsumer<TMessage> where TMessage : class
        {
            _services.AddScoped(typeof(TConsumer));
            _consumers.Add((typeof(TConsumer), typeof(TMessage)));
            return this;
        }

        public IEnumerable<(Type ConsumerType, Type MessageType)> GetConsumers()
        {
            return _consumers;
        }

        public void Build()
        {
            _services.AddSingleton(this);
            _services.AddHostedService<RedisSubBackgroundService>();
        }
    }
}
