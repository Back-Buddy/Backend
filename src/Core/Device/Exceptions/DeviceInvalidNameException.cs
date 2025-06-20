using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class DeviceInvalidNameException() : AbstractBaseException("Device.InvalidName", "The device name must match the pattern!", StatusCodes.Status400BadRequest)
    {
    }
}
