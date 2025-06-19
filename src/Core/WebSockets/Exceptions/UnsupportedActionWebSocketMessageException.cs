using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.WebSockets.Exceptions
{
    public class UnsupportedActionWebSocketMessageException : AbstractBaseException
    {
        public UnsupportedActionWebSocketMessageException() : base("WebSocket.UnsupportedAction", $"Unsupportet Action!", StatusCodes.Status400BadRequest)
        {
        }
    }
}
