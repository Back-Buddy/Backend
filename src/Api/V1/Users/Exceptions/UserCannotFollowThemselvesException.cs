using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Users.Exceptions
{
    public class UserCannotFollowThemselvesException : AbstractBaseException
    {
        public UserCannotFollowThemselvesException() : base("User.CannotFollowThemselvesException", "User cannot follow themselves.", StatusCodes.Status400BadRequest)
        {
        }
    }
}
