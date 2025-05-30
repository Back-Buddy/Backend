using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Notification.Exceptions
{
    public class FcmTokenIsNullException : AbstractBaseException
    {
        public FcmTokenIsNullException() : base("Notification.FcmTokenIsNull", "The FCM token cannot be null or empty.", StatusCodes.Status400BadRequest)
        {
        }
    }
}