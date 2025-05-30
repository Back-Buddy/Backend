
using BackBuddy.Api.Service.V1.WebSockets.Services;
using Microsoft.OpenApi.Writers;
using StackExchange.Redis;
using System.Reflection;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.Database.Redis
{
    public class RedisSubBackgroundService(IConnectionMultiplexer connectionMultiplexer, RedisSubBuilder redisSubBuilder, IServiceScopeFactory serviceScopeFactory, ILogger<RedisSubBackgroundService> logger) : BackgroundService
    {
        public const string ChannelPrefix = "websocket:";

        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer;
        private readonly ILogger<RedisSubBackgroundService> _logger = logger;

        /// <summary>
        /// Mapping of channel names to consumer types.
        /// Key: Channel name (e.g., "websocket:WebSocketSendMessage")
        /// Value: List of consumer types that handle messages for that channel.
        /// This is generated from the consumers registered in the RedisSubBuilder.
        /// </summary>
        private readonly Dictionary<string, List<Type>> _messageConsumerMapping = GenerateConsumerMappingDictionary(redisSubBuilder.GetConsumers());

        /// <summary>
        /// Mapping of consumer types to their corresponding message types.
        /// Key: Consumer type (e.g., WebSocketSendMessageConsumer)
        /// Value: Message type that the consumer handles (e.g., WebSocketSendMessage).
        /// This is generated from the consumers registered in the RedisSubBuilder.
        /// </summary>
        private readonly Dictionary<Type, Type> _consumerMessageTypeMapping = GenerateConsumerMessageDictionary(redisSubBuilder.GetConsumers());

        /// <summary>
        /// Mapping of consumer types to their Consume method.
        /// Key: Consumer type (e.g., WebSocketSendMessageConsumer)
        /// Value: MethodInfo for the Consume method that takes the message type as a parameter.
        /// </summary>
        private readonly Dictionary<Type, MethodInfo> _consumerMethods = GenerateConsumerMethodDictionary(redisSubBuilder.GetConsumers());

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ISubscriber subscriber = _connectionMultiplexer.GetSubscriber();
            foreach ((string channelName, List<Type> consumers) in _messageConsumerMapping)
            {
                _logger.LogInformation("Subscribing to channel {ChannelName} with {ConsumerCount} consumers", channelName, consumers.Count);
                await subscriber.SubscribeAsync(new RedisChannel(channelName, RedisChannel.PatternMode.Literal), async (channel, message) => await Handle(_serviceScopeFactory.CreateScope(), channel.ToString() ?? string.Empty, message, consumers));
            }
        }

        internal async Task Handle(IServiceScope scope, string channelName, RedisValue message, IEnumerable<Type> consumers)
        {
            if (message.IsNullOrEmpty)
            {
                _logger.LogWarning("Received empty message on channel {ChannelName}", channelName);
                return;
            }
            if (!consumers.Any()) return;

            foreach (Type consumerType in consumers)
            {
                try
                {
                    object consumer = scope.ServiceProvider.GetService(consumerType) ?? throw new InvalidOperationException("Consumer not registered! Use the RedisSubBuilder!");

                    Type messageType = _consumerMessageTypeMapping[consumerType];
                    object deserializedMessage = JsonSerializer.Deserialize(message!, messageType, WebSocketService.JsonOptions) ?? throw new JsonException("Message cloud not be parsed!");

                    MethodInfo method = _consumerMethods[consumerType];
                    var task = (Task?)method.Invoke(consumer, [deserializedMessage]);
                    if (task != null)
                        await task;
                }
                catch (TargetInvocationException ex)
                {
                    _logger.LogError(ex.InnerException, "Error invoking Consume method for consumer {ConsumerType} on channel {ChannelName}", consumerType.Name, channelName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message for consumer {ConsumerType} on channel {ChannelName}", consumerType.Name, channelName);
                }
            }
        }

        internal static Dictionary<string, List<Type>> GenerateConsumerMappingDictionary(IEnumerable<(Type ConsumerType, Type MessageType)> consumers)
        {
            Dictionary<string, List<Type>> result = [];
            foreach ((Type consumerType, Type messageType) in consumers)
            {
                string channelName = messageType.GetRedisChannelKey();
                if (!result.ContainsKey(channelName))
                    result[channelName] = [];
                result[channelName].Add(consumerType);
            }
            return result;
        }

        internal static Dictionary<Type, Type> GenerateConsumerMessageDictionary(IEnumerable<(Type ConsumerType, Type MessageType)> consumers)
        {
            Dictionary<Type, Type> result = [];
            foreach ((Type consumerType, Type messageType) in consumers)
            {
                result.Add(consumerType, messageType);
            }
            return result;
        }

        internal static Dictionary<Type, MethodInfo> GenerateConsumerMethodDictionary(IEnumerable<(Type ConsumerType, Type MessageType)> consumers)
        {
            return consumers
            .Select(consumer => consumer.ConsumerType)
            .ToDictionary(consumerType => consumerType, consumerType => consumerType.GetMethod("Consume") ?? throw new InvalidOperationException($"Consume method not found for {consumerType.Name}"));
        }
    }
}
