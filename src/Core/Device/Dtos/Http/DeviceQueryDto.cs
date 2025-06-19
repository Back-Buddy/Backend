using System.ComponentModel;

namespace BackBuddy.Core.Library.Device.Dtos.Http
{
    public record DeviceQueryDto
    {
        public bool? Active { get; init; }
        
        [DefaultValue(true)]
        public bool Descending { get; init; } = true;
    }
}
