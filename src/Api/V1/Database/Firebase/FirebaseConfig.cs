using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Api.Service.V1.Database.Firebase
{
    public record FirebaseConfig
    {
        [Required]
        public required string Secret { get; init; }
        [Required]
        public required string ProjectId { get; init; }
    }
}
