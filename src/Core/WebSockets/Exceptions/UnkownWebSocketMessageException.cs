using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.WebSockets.Exceptions
{
    public class UnkownWebSocketMessageException(string rawType) : AbstractBaseException("WebSocket.UnkownMessage", $"Unkown Message! Type: {rawType}", StatusCodes.Status400BadRequest)
    {
    }
}
