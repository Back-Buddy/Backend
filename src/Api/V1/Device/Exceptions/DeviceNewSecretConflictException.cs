using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class DeviceNewSecretConflictException : AbstractBaseException
    {
        public DeviceNewSecretConflictException() : base("Device.NewSecretConflict", "The provided new secret could not be stored because the cached secret is different. Please wait for another secret change notification.", StatusCodes.Status409Conflict)
        {
        }
    }
}