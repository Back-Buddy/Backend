using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.WebSockets.Exceptions
{
    public class UnkownWebSocketMessageException(string rawType) : AbstractBaseException("WebSocket.UnkownMessage", $"Unkown Message! Type: {rawType}", StatusCodes.Status400BadRequest)
    {
    }
}
