using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class DeviceUnauthorizedException : AbstractBaseException
    {
        public DeviceUnauthorizedException() : base("Device.Unauthorized", "The device is not authorized", StatusCodes.Status401Unauthorized)
        {
        }
    }
}
