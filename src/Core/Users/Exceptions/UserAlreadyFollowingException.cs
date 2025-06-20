using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Users.Exceptions
{
    public class UserAlreadyFollowingException : AbstractBaseException
    {
        public UserAlreadyFollowingException() : base("User.AlreadyFollowing", "Current user is already following the specified target user.", StatusCodes.Status409Conflict)
        {
        }
    }
}