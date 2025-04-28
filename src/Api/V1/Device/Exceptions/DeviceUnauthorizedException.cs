using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class DeviceUnauthorizedException : AbstractBaseException
    {
        public DeviceUnauthorizedException() : base("Device.Unauthorized", "The device is not authorized", StatusCodes.Status401Unauthorized)
        {
        }
    }
}
