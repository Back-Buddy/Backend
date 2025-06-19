using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class ReportNotFoundException : AbstractBaseException
    {
        public ReportNotFoundException() : base("Device.ReportNotFound", "The report was not found", StatusCodes.Status404NotFound)
        {
        }
    }
}
