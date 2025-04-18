using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Enums;

namespace BackBuddy.Api.Service.V1.Auth
{
    public class TestMessage : IWebSocketMessageDto
    {
        public WebSocketMessageType MessageType => WebSocketMessageType.StatusMessage;

        public required string Status { get; set; }

        public bool IsToSend() => false;
    }
}
