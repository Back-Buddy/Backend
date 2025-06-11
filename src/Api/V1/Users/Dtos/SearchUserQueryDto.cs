using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Api.Service.V1.Users.Dtos
{
    public record SearchUserQueryDto
    {
        [Required]
        [MinLength(1)]
        public required string SearchTerm { get; init; }

        [Range(1, 100)]
        [DefaultValue(10)]
        public int Limit { get; init; } = 10;
    }
}
