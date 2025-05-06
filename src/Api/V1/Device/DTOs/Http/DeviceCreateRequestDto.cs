using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Api.Service.V1.Device.DTOs.Http
{
    public record DeviceCreateRequestDto
    {
        [Required]
        public required string Name { get; init; }
    }
}
