using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using System.Net.WebSockets;

namespace BackBuddy.Api.Service.V1.WebSockets.Services
{
    public interface IWebSocketService
    {
        Task<bool> OnConnect(WebSocket webSocket, string authorization);
        Task OnDisconnect(WebSocket webSocket, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure);
        Task OnReceive(WebSocket webSocket, string payload);
        Task<bool> SendMessage(Guid deviceId, IWebSocketMessageDto message);
    }
}
