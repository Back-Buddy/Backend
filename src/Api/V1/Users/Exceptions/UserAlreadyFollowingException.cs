using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Users.Exceptions
{
    public class UserAlreadyFollowingException : AbstractBaseException
    {
        public UserAlreadyFollowingException()
            : base("User.AlreadyFollowing", "Current user is already following the specified target user.", StatusCodes.Status409Conflict)
        {
        }
    }
}