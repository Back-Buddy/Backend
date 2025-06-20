using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class ReportInvalidTimeFilterException : AbstractBaseException
    {
        public ReportInvalidTimeFilterException() : base("Device.ReportInvalidTimeFilter", "The time filter for the report is invalid! EndTime must be greater than StartTime", StatusCodes.Status400BadRequest)
        {
        }
    }
}
