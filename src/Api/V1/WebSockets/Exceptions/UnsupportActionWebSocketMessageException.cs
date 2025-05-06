using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.WebSockets.Exceptions
{
    public class UnsupportActionWebSocketMessageException : AbstractBaseException
    {
        public UnsupportActionWebSocketMessageException() : base("WebSocket.UnsupportetAction", $"Unsupportet Action!", StatusCodes.Status400BadRequest)
        {
        }
    }
}
