using BackBuddy.Core.Library.Utilities;

namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserGetOutgoingRelationsRequestMessage
    {
        public required string UserId { get; init; }
        public required PageRequestDto Page { get; init; }
    }

    public record UserGetOutgoingRelationsResponseMessage
    {
        public required Page<List<string>> Relations { get; init; }
    }
}
