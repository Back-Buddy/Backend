using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Users.Exceptions
{
    public class UserCannotFollowThemselvesException : AbstractBaseException
    {
        public UserCannotFollowThemselvesException() : base("User.CannotFollowThemselves", "User cannot follow themselves.", StatusCodes.Status400BadRequest)
        {
        }
    }
}
