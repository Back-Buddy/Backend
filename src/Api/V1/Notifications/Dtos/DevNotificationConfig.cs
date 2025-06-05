using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Api.Service.V1.Notifications.Dtos
{
    public record DevNotificationConfig
    {
        [Required]
        public required string Uri { get; init; }
    }
}
