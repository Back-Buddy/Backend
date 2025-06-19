using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Api.Service.V1.Exceptions
{
    public class UnauthorizedException : AbstractBaseException
    {
        public UnauthorizedException(): base("System.Unauthorized", "Unauthorized", StatusCodes.Status401Unauthorized)
        {}
    }
}
