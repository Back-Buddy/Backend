using BackBuddy.Api.Service.V1.WebSockets.Enums;

namespace BackBuddy.Core.Library.WebSockets.Dtos
{
    public interface IWebSocketMessageDto
    {
        WebSocketMessageType MessageType { get; }

        bool IsToSend { get; }
    }
}
