using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class DeviceNameIsNotUniqueException(string name) : AbstractBaseException("Device.NameIsNotUnique", $"The device name {name} is not unique", StatusCodes.Status409Conflict)
    {
    }
}
