using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class DeviceLogNotFoundException : AbstractBaseException
    {
        public DeviceLogNotFoundException() : base("Device.Log.NotFound", "The device log was not found", StatusCodes.Status404NotFound)
        {
        }
    }
}
