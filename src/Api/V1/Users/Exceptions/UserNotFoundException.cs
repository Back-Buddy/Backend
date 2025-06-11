using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Users.Exceptions
{
    public class UserNotFoundException : AbstractBaseException
    {
        public UserNotFoundException() : base("User.NotFound", "User with ID not found.", StatusCodes.Status404NotFound)
        {
        }
    }
}
