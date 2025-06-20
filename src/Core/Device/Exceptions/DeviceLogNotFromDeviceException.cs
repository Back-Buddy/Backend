using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class DeviceLogNotFromDeviceException() : AbstractBaseException("Device.Log.NotFromDevice", $"The log does not belong to the device.", StatusCodes.Status409Conflict)
    {
    }
}
