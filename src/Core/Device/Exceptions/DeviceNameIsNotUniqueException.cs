using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class DeviceNameIsNotUniqueException() : AbstractBaseException("Device.NameIsNotUnique", $"The device name is not unique", StatusCodes.Status409Conflict)
    {
    }
}
