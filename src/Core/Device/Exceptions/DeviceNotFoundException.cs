using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class DeviceNotFoundException : AbstractBaseException
    {
        public DeviceNotFoundException() : base("Device.NotFound", "The device was not found", StatusCodes.Status404NotFound)
        {
        }
    }
}
