using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Users.Exceptions
{
    public class UserNotFollowingException : AbstractBaseException
    {
        public UserNotFollowingException()
            : base("User.NotFollowing", "Current user is not following the specified target user.", StatusCodes.Status409Conflict)
        {
        }
    }
}
