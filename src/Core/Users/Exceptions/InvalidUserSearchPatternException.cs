using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Users.Exceptions
{
    public class InvalidUserSearchPatternException : AbstractBaseException
    {
        public InvalidUserSearchPatternException() : base("User.InvalidSearchPattern", "Invalid Search Pattern! Regex: ^[a-zA-Z0-9 ]+$", StatusCodes.Status400BadRequest)
        {
        }
    }
}
