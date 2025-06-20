using BackBuddy.Core.Library.Users.Dtos.Http;
using BackBuddy.Core.Library.Users.Enums;

namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserSearchRequestMessage
    {
        public required SearchUserQueryDto Query { get; init; }
        public required UserExpandType UserExpandType { get; init; }
    }

    public record UserSearchResponseMessage
    {
        public required IEnumerable<UserDto> Users { get; init; }
    }
}
