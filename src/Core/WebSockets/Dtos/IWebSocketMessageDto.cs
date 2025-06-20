using BackBuddy.Core.Library.WebSockets.Enums;

namespace BackBuddy.Core.Library.WebSockets.Dtos
{
    public interface IWebSocketMessageDto
    {
        WebSocketMessageType MessageType { get; }

        bool IsToSend { get; }
    }
}
