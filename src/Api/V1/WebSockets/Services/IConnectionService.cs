using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace BackBuddy.Api.Service.V1.WebSockets.Services
{
    public interface IConnectionService
    {
        ConcurrentDictionary<Guid, WebSocket> GetAllConnections();
        WebSocket? GetWebSocket(Guid deviceId);
        List<WebSocket> GetWebSockets();
        List<Guid> GetAllDevices();
        bool AddWebSocket(WebSocket webSocket, Guid deviceId);
        Task RemoveWebSocket(WebSocket webSocket, string? reason = null, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure);
        Guid? GetDevice(WebSocket webSocket);
    }
}
