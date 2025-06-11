using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Users.Exceptions
{
    public class InvalidUserSearchPatternException : AbstractBaseException
    {
        public InvalidUserSearchPatternException() : base("User.InvalidSearchPattern", "Invalid Search Pattern! Regex: ^[a-zA-Z0-9 ]+$", StatusCodes.Status400BadRequest)
        {
        }
    }
}
