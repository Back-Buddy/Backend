using BackBuddy.Api.Service.V1.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace BackBuddy.Api.Service.V1.ExceptionHandlers
{
    public class AbstractBaseExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if(exception is AbstractBaseException baseException)
            {
                await baseException.WriteToResponse(httpContext.Response);
                return true;
            }

            InternalServerErrorException internalServerError = new();
            await internalServerError.WriteToResponse(httpContext.Response);
            return true;
        }
    }
}
