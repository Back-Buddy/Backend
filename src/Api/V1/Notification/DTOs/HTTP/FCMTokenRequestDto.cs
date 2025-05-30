using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Api.Service.V1.Notification.DTOs.Http
{
    public record FCMTokenRequestDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "The FCM token cannot be null or empty.")]
        public required string FCMToken { get; set; }
    }
}
