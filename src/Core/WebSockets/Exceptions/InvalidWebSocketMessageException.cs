using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.WebSockets.Exceptions
{
    public class InvalidWebSocketMessageException : AbstractBaseException
    {
        public InvalidWebSocketMessageException() : base("WebSocket.InvalidMessage", "Invalid Message", StatusCodes.Status400BadRequest)
        {
        }
    }
}
