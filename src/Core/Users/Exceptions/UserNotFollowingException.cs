using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Users.Exceptions
{
    public class UserNotFollowingException : AbstractBaseException
    {
        public UserNotFollowingException() : base("User.NotFollowing", "Current user is not following the specified target user.", StatusCodes.Status409Conflict)
        {
        }
    }
}
