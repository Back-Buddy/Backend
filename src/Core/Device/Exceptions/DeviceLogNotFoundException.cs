using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class DeviceLogNotFoundException : AbstractBaseException
    {
        public DeviceLogNotFoundException() : base("Device.Log.NotFound", "The device log was not found", StatusCodes.Status404NotFound)
        {
        }
    }
}
