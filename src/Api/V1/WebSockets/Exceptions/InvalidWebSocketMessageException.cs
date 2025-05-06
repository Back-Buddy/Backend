using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.WebSockets.Exceptions
{
    public class InvalidWebSocketMessageException : AbstractBaseException
    {
        public InvalidWebSocketMessageException() : base("WebSocket.InvalidMessage", "Invalid Message", StatusCodes.Status400BadRequest)
        {
        }
    }
}
