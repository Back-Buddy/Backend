using System.ComponentModel;

namespace BackBuddy.Api.Service.V1.Device.DTOs.Http
{
    public record DeviceQueryDto
    {
        public bool? Active { get; init; }
        
        [DefaultValue(true)]
        public bool Descending { get; init; } = true;
    }
}
