using BackBuddy.Api.Service.V1.Auth;
using BackBuddy.Api.Service.V1.WebSockets.Enums;

namespace BackBuddy.Api.Service.V1.WebSockets.Mapper
{
    public static class WebSocketMessageTypeMapper
    {
        public static Type GetMessageType(this WebSocketMessageType type)
        {
            return type switch
            {
                WebSocketMessageType.StatusMessage => typeof(TestMessage),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
