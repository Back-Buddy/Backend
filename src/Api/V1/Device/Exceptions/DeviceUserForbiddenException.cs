using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class DeviceUserForbiddenException : AbstractBaseException
    {
        public DeviceUserForbiddenException() : base("Device.UserForbidden", "The user is forbidden to access this device", StatusCodes.Status403Forbidden)
        {
        }
    }
}
