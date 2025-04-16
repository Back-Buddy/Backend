using BackBuddy.Api.Service.V1.Exceptions;
using BackBuddy.Api.Service.V1.WebSockets.Converter;
using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using BackBuddy.Api.Service.V1.WebSockets.Exceptions;
using MassTransit;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackBuddy.Api.Service.V1.WebSockets.Services
{
    public class WebSocketService(IConnectionService _connectionService, IPublishEndpoint _publishEndpoint) : IWebSocketService
    {

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

        public async Task OnDisconnect(WebSocket webSocket)
        {
            await _connectionService.RemoveWebSocket(webSocket, "Disconnected");
        }

        public async Task OnReceive(WebSocket webSocket, string payload)
        {
            IWebSocketMessageDto message = JsonSerializer.Deserialize<IWebSocketMessageDto>(payload, _options) ?? throw new InvalidWebSocketMessageException();
            if (message.IsToSend())
                throw new UnsupportActionWebSocketMessageException();
            //TODO: Other Exception (Internal Server Error or something else) | Log the error
            Guid deviceId = _connectionService.GetDevice(webSocket) ?? throw new UnauthorizedException();


            WebSocketMessageReceive webSocketMessageReceive= new()
            {
                DeviceId = deviceId,
                Message = message
            };

            await _publishEndpoint.Publish(webSocketMessageReceive);
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
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            return true;
        }
    }
}
