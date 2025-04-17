using BackBuddy.Api.Service.V1.Exceptions;
using BackBuddy.Api.Service.V1.WebSockets.Converter;
using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Exceptions;
using BackBuddy.Api.Service.V1.WebSockets.Mapper;
using MassTransit;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackBuddy.Api.Service.V1.WebSockets.Services
{
    public class WebSocketService(IConnectionService _connectionService, IPublishEndpoint _publishEndpoint) : IWebSocketService
    {
        private static readonly ConcurrentDictionary<Enums.WebSocketMessageType, (Type GenericType, Func<Guid, IWebSocketMessageDto, object> Factory)> _messageFactoryCache = [];

        private readonly static JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new WebSocketMessageConverter(), new JsonStringEnumConverter() }
        };

        public async Task<bool> OnConnect(WebSocket webSocket, string authorization)
        {
            //TODO: Add authorization
            //TODO: Read DeviceId from the token
            Guid deviceId = Guid.NewGuid();
            if (!_connectionService.AddWebSocket(webSocket, deviceId))
            {
                // If the user is already connected, we close the old
                WebSocket? toClose = _connectionService.GetWebSocket(deviceId);
                if (toClose != null)
                    await _connectionService.RemoveWebSocket(toClose, "Reconnected");

                return _connectionService.AddWebSocket(webSocket, deviceId);
            }
            return true;
        }

        public async Task OnDisconnect(WebSocket webSocket, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure)
        {
            await _connectionService.RemoveWebSocket(webSocket, "Disconnected", closeStatus);
        }


        public async Task OnReceive(WebSocket webSocket, string payload)
        {
            IWebSocketMessageDto message = JsonSerializer.Deserialize<IWebSocketMessageDto>(payload, _options) ?? throw new InvalidWebSocketMessageException();
            if (message.IsToSend())
                throw new UnsupportActionWebSocketMessageException();

            Guid deviceId = _connectionService.GetDevice(webSocket) ?? throw new UnauthorizedException();

            (Type genericType, Func<Guid, IWebSocketMessageDto, object> factory) = _messageFactoryCache.GetOrAdd(
                message.MessageType,
                msgType =>
                {
                    Type payloadType = msgType.GetMessageType();
                    Type genType = typeof(WebSocketMessageReceive<>).MakeGenericType(payloadType);

                    object factoryFunc(Guid id, IWebSocketMessageDto msg)
                    {
                        return Activator.CreateInstance(genType, [id, msg])
                            ?? throw new InvalidOperationException($"Failed to create instance of {genType.Name}");
                    }

                    return (genType, factoryFunc);
                });

            object messageReceive = factory(deviceId, message);
            await _publishEndpoint.Publish(messageReceive, genericType);
        }

        public async Task<bool> SendMessage(Guid deviceId, IWebSocketMessageDto message)
        {
            if (!message.IsToSend())
                throw new UnsupportActionWebSocketMessageException();
            WebSocket? webSocket = _connectionService.GetWebSocket(deviceId);
            if (webSocket == null)
                return false;
            string payload = JsonSerializer.Serialize(message, _options);
            byte[] buffer = Encoding.UTF8.GetBytes(payload);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
            return true;
        }
    }
}
