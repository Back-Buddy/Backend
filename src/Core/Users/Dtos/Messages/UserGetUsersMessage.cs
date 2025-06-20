using BackBuddy.Core.Library.Users.Enums;

namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserGetUsersRequestMessage
    {
        public required List<string> UserIds { get; init; }
        public required UserExpandType UserExpandType { get; init; }
    }

    public record UserGetUsersResponseMessage
    {
        public required List<UserDto> Users { get; init; }
    }
}
