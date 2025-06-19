using BackBuddy.Core.Library.WebSockets;
using StackExchange.Redis;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.Database.Redis
{
    public interface IPublisher
    {
        Task PublishAsync<T>(T payload, CommandFlags commandFlags = CommandFlags.None) where T : class;
    }

    public class Publisher(IConnectionMultiplexer connectionMultiplexer) : IPublisher
    {
        private readonly ISubscriber _subscriber = connectionMultiplexer.GetSubscriber();

        public async Task PublishAsync<T>(T payload, CommandFlags commandFlags = CommandFlags.None) where T : class
        {
            string jsonPayload = JsonSerializer.Serialize(payload, WebSocketConstants.JsonOptions);
            await _subscriber.PublishAsync(new RedisChannel(typeof(T).GetRedisChannelKey(), RedisChannel.PatternMode.Literal), jsonPayload, commandFlags);
        }
    }
}
