using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Users.Exceptions
{
    public class UserAlreadyFollowingException : AbstractBaseException
    {
        public UserAlreadyFollowingException()
            : base("User.Following", $"User is already following user!", StatusCodes.Status409Conflict)
        {
        }
    }
}