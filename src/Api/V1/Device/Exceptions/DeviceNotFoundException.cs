using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class DeviceNotFoundException : AbstractBaseException
    {
        public DeviceNotFoundException() : base("Device.NotFound", "The device was not found", StatusCodes.Status404NotFound)
        {
        }
    }
}
