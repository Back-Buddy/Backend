using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Users.Exceptions
{
    public class UserCannotFollowThemselvesException : AbstractBaseException
    {
        public UserCannotFollowThemselvesException() : base("User.CannotFollowThemselves", "User cannot follow themselves.", StatusCodes.Status400BadRequest)
        {
        }
    }
}
