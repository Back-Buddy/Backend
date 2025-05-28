using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Api.Service.V1.Notification.DTOs.Http
{
    public class FCMTokenRequestDto
    {
        [Required]
        public required string FCMToken { get; set; } = string.Empty;
    }
}
