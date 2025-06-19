using BackBuddy.Core.Library.Users.Enums;

namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserGetByIdRequestMessage
    {
        public required string UserId { get; init; }
        public required UserExpandType UserExpandType { get; init; }
    }

    public record UserGetByIdResponseMessage
    {
        public required UserDto User { get; init; }
    }
}
