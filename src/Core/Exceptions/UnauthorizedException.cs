using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Exceptions
{
    public class UnauthorizedException : AbstractBaseException
    {
        public UnauthorizedException() : base("System.Unauthorized", "Unauthorized", StatusCodes.Status401Unauthorized)
        { }
    }
}
