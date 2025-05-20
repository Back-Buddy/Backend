using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using System.Net.WebSockets;

namespace BackBuddy.Api.Service.V1.WebSockets.Services
{
    public interface IWebSocketService
    {
        Task<Guid> OnAuthorization(string authorization);
        Task<bool> OnConnect(WebSocket webSocket, Guid deviceId);
        Task OnDisconnect(WebSocket webSocket, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure);
        Task OnReceive(WebSocket webSocket, string payload);
        Task<bool> SendMessage(Guid deviceId, IWebSocketMessageDto message);
        Task<bool> IsDeviceConnected(Guid deviceId);
    }
}
