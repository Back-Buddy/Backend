using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Exceptions
{
    public class InternalServerErrorException : AbstractBaseException
    {
        public InternalServerErrorException() : base("System.InternalServerError", "Please try is later.", StatusCodes.Status500InternalServerError)
        {
        }
    }
}
