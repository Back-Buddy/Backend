using BackBuddy.Api.Service.V1.WebSockets.Dtos;
using System.Net.WebSockets;

namespace BackBuddy.Api.Service.V1.WebSockets.Services
{
    public interface IWebSocketService
    {
        Task<bool> OnConnect(WebSocket webSocket, string authorization);
        Task OnDisconnect(WebSocket webSocket);
        Task OnReceive(WebSocket webSocket, string payload);
        Task<bool> SendMessage(Guid deviceId, IWebSocketMessageDto message);
    }
}
