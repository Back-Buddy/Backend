using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Api.Service.V1.WebSockets.Dtos
{
    public record ConnectedDeviceConfig
    {
        [Required]
        public required TimeSpan PresenceTimeout { get; init; }
        [Required]
        public required TimeSpan MetaTimeout { get; init; }
        [Required]
        public required TimeSpan HeartbeatInterval { get; init; }
    }
}
