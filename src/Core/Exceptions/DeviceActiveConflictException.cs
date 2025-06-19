using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Api.Service.V1.Exceptions
{
    public class DeviceActiveConflictException : AbstractBaseException
    {
        public DeviceActiveConflictException() : base("Device.ActiveConflict", "Only one device can be active at a time.", StatusCodes.Status409Conflict)
        {
        }
    }
}