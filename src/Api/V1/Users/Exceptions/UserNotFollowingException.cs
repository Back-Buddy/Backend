using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Users.Exceptions
{
    public class UserNotFollowingException : AbstractBaseException
    {
        public UserNotFollowingException()
            : base("User.NotFollowing", "User is not following user!", StatusCodes.Status400BadRequest)
        {
        }
    }
}
