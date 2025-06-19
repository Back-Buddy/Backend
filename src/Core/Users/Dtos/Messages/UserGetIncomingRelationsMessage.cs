using BackBuddy.Core.Library.Utilities;

namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserGetIncomingRelationsRequestMessage
    {
        public required string UserId { get; init; }
        public required PageRequestDto Page { get; init; }
    }

    public record UserGetIncomingRelationsResponseMessage
    {
        public required Page<List<string>> Relations { get; init; }
    }
}
