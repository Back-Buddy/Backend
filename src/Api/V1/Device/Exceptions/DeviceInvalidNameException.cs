using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class DeviceInvalidNameException(string pattern) : AbstractBaseException("Device.InvalidName", $"The device name must match the pattern {pattern}", StatusCodes.Status400BadRequest)
    {
    }
}
