namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record GetStrongFollowRelationsAndAllFollowingsRequestMessage
    {
        public required string UserId { get; init; }
    }

    public record GetStrongFollowRelationsAndAllFollowingsResponseMessage
    {
        public required IEnumerable<string> StrongRelations { get; init; }
        public required IEnumerable<string> Following { get; init; }
    }
}