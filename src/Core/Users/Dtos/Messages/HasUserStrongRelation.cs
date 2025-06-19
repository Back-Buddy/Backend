namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record HasUserStrongRelationRequestMessage
    {
        public required string UserId { get; init; }
        public required string TargetUserId { get; init; }
    }
    
    public record HasUserStrongRelationResponseMessage
    {
        public required bool HasStrongRelation { get; init; }
    }
}