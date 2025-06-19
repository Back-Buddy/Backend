using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Core.Library.Device.Dtos.Http
{
    public record DeviceCreateRequestDto
    {
        [Required]
        public required string Name { get; init; }
    }
}
