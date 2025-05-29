using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.DTOs.Queue;
using BackBuddy.Api.Service.V1.Exceptions;
using BackBuddy.Api.Service.V1.WebSockets.Converter;
using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Entities;
using BackBuddy.Api.Service.V1.WebSockets.Exceptions;
using BackBuddy.Api.Service.V1.WebSockets.Mapper;
using BackBuddy.Api.Service.V1.WebSockets.Repositories;
using MassTransit;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackBuddy.Api.Service.V1.WebSockets.Services
{
    public class WebSocketService(IConnectionService connectionService, IConnectedDeviceRepository connectedDeviceRepository, IRequestClient<DeviceAuthorizeRequestMessage> deviceAuthRequestClient, IPublishEndpoint publishEndpoint) : IWebSocketService
    {
        private static readonly ConcurrentDictionary<Enums.WebSocketMessageType, (Type GenericType, Func<Guid, IWebSocketMessageDto, object> Factory)> _messageFactoryCache = [];

        internal readonly static JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new WebSocketMessageConverter(), new JsonStringEnumConverter() }
        };

        private readonly IConnectedDeviceRepository _connectedDeviceRepository = connectedDeviceRepository;
        private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
        private readonly IConnectionService _connectionService = connectionService;
        private readonly IRequestClient<DeviceAuthorizeRequestMessage> _deviceAuthRequestClient = deviceAuthRequestClient;

        public async Task<Guid> OnAuthorization(string authorization)
        {
            DeviceAuthorizeRequestMessage authorizeRequestMessage = new()
            {
                Secret = authorization
            };

            Response<DeviceDto> response = await _deviceAuthRequestClient.GetResponse<DeviceDto>(authorizeRequestMessage);
            DeviceDto device = response.Message;
            return device.Id;
        }

        public async Task<bool> OnConnect(WebSocket webSocket, Guid deviceId)
        {
            if (!_connectionService.AddWebSocket(webSocket, deviceId))
            {
                // If the user is already connected, we close the old
                WebSocket? toClose = _connectionService.GetWebSocket(deviceId);
                if (toClose != null)
                    await _connectionService.RemoveWebSocket(toClose, "Reconnected", WebSocketCloseStatus.NormalClosure);

                return _connectionService.AddWebSocket(webSocket, deviceId);
            }

            await _connectedDeviceRepository.Add(deviceId, new ConnectedDevice { ConnectedAt = DateTime.UtcNow });

            WebSocketConnectedMessage connectedMessage = new()
            {
                DeviceId = deviceId
            };

            await _publishEndpoint.Publish(connectedMessage);
            return true;
        }

        public async Task OnDisconnect(WebSocket webSocket, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure)
        {
            Guid? deviceId = _connectionService.GetDevice(webSocket);
            if (deviceId == null)
                return;

            await _connectedDeviceRepository.Remove(deviceId.Value);
            await _connectionService.RemoveWebSocket(webSocket, "Disconnected", closeStatus);
        }


        public async Task OnReceive(WebSocket webSocket, string payload)
        {
            IWebSocketMessageDto message = JsonSerializer.Deserialize<IWebSocketMessageDto>(payload, JsonOptions) ?? throw new InvalidWebSocketMessageException();
            if (message.IsToSend)
                throw new UnsupportedActionWebSocketMessageException();

            Guid deviceId = _connectionService.GetDevice(webSocket) ?? throw new UnauthorizedException();
            (Type genericType, Func<Guid, IWebSocketMessageDto, object> factory) = _messageFactoryCache.GetOrAdd(
                message.MessageType,
                msgType =>
                {
                    Type payloadType = msgType.GetMessageType();
                    Type genType = typeof(WebSocketMessageReceive<>).MakeGenericType(payloadType);
                    var factoryFunc = CreateFactory(payloadType, genType);
                    return (genType, factoryFunc);
                });

            object messageReceive = factory(deviceId, message);
            await _publishEndpoint.Publish(messageReceive, genericType);
        }

        public async Task<bool> SendMessage(Guid deviceId, IWebSocketMessageDto message)
        {
            if (!message.IsToSend)
                throw new UnsupportedActionWebSocketMessageException();
            WebSocket? webSocket = _connectionService.GetWebSocket(deviceId);
            if (webSocket == null)
                return false;

            string payload = JsonSerializer.Serialize(message, message.MessageType.GetMessageType(), JsonOptions);


            byte[] buffer = Encoding.UTF8.GetBytes(payload);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
            return true;
        }

        public async Task<bool> IsDeviceConnected(Guid deviceId)
        {
            ConnectedDevice? connectedDevice = await _connectedDeviceRepository.Get(deviceId);
            return connectedDevice != null;
        }

        private static Func<Guid, IWebSocketMessageDto, object> CreateFactory(Type genericPayloadType, Type genericType)
        {
            ParameterExpression idParam = Expression.Parameter(typeof(Guid), "deviceId");
            ParameterExpression msgParam = Expression.Parameter(typeof(IWebSocketMessageDto), "message");

            UnaryExpression castedMsg = Expression.Convert(msgParam, genericPayloadType);

            ConstructorInfo ctor = genericType.GetConstructor([typeof(Guid), genericPayloadType]) ?? throw new InvalidOperationException($"Constructor not found on {genericType.Name}");

            NewExpression newExpr = Expression.New(ctor, idParam, castedMsg);

            UnaryExpression castResult = Expression.Convert(newExpr, typeof(object));

            var lambda = Expression.Lambda<Func<Guid, IWebSocketMessageDto, object>>(castResult, idParam, msgParam);
            return lambda.Compile();
        }
    }
}
