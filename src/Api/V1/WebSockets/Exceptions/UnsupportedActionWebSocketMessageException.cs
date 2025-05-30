using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.WebSockets.Exceptions
{
    public class UnsupportedActionWebSocketMessageException : AbstractBaseException
    {
        public UnsupportedActionWebSocketMessageException() : base("WebSocket.UnsupportedAction", $"Unsupportet Action!", StatusCodes.Status400BadRequest)
        {
        }
    }
}
