using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class DeviceLogNotFromDeviceException() : AbstractBaseException("Device.Log.NotFromDevice", $"The log does not belong to the device.", StatusCodes.Status409Conflict)
    {
    }
}
