using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace BackBuddy.Api.Service.V1.WebSockets.Services
{
    public class ConnectionService : IConnectionService
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = [];

        public bool AddWebSocket(WebSocket webSocket, Guid deviceId)
        {
            return _sockets.TryAdd(deviceId, webSocket);
        }

        public ConcurrentDictionary<Guid, WebSocket> GetAllConnections()
        {
            return _sockets;
        }

        public List<Guid> GetAllDevices()
        {
            return [.. _sockets.Keys];
        }

        public Guid? GetDevice(WebSocket webSocket)
        {
            Guid deviceId = _sockets.FirstOrDefault(x => x.Value == webSocket).Key;
            if (deviceId == Guid.Empty)
                return null;
            return deviceId;
        }

        public WebSocket? GetWebSocket(Guid deviceId)
        {
            return _sockets.TryGetValue(deviceId, out WebSocket? webSocket) ? webSocket : null;
        }

        public List<WebSocket> GetWebSockets()
        {
            return [.. _sockets.Values];
        }

        public async Task RemoveWebSocket(WebSocket webSocket, string? reason = null, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure)
        {
            KeyValuePair<Guid, WebSocket> registeredWebsocket = _sockets.FirstOrDefault(x => x.Value == webSocket);

            if (registeredWebsocket.Key != Guid.Empty)
                _sockets.TryRemove(registeredWebsocket.Key, out _);

            if (webSocket.State != WebSocketState.Aborted)
            {
                await webSocket.CloseAsync(closeStatus: closeStatus, statusDescription: reason, cancellationToken: CancellationToken.None);
            }
        }
    }
}
