using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Api.Service.V1.Device.DTOs.Http
{
    public class ReportCreateDto
    {
        [Required]
        public required Guid DeviceId { get; init; }
        [Required]
        public required DateTime StartTime { get; init; }
        [Required]
        public required DateTime EndTime { get; init; }
    }
}
