using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class DeviceUserForbiddenException : AbstractBaseException
    {
        public DeviceUserForbiddenException() : base("Device.UserForbidden", "The user is forbidden to access this device", StatusCodes.Status403Forbidden)
        {
        }
    }
}
