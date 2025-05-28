using Microsoft.AspNetCore.Mvc;
using BackBuddy.Api.Service.V1.Notification.DTOs.Http;
using BackBuddy.Api.Service.V1.Notification.Services;
using BackBuddy.Api.Service.V1.Auth.Extensions;

namespace BackBuddy.Api.Service.V1.Notification.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class NotificationController(INotificationService notificationService) : ControllerBase
    {
        [HttpPost("{FCMToken}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SetFcmToken([FromBody] FCMTokenRequestDto request)
        {
            await notificationService.SetFcmToken(this.GetUserId(), request.FCMToken);
            return NoContent();
        }   
    }
}