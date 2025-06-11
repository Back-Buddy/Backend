using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Users.Exceptions
{
    public class UserCannotFollowThemselfException : AbstractBaseException
    {
        public UserCannotFollowThemselfException() : base("User.CannotFollowThemselfException", "User cannot follow themselves.", StatusCodes.Status400BadRequest)
        {
        }
    }
}
