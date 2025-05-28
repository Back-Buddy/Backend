using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class ReportInvalidTimeFilterException : AbstractBaseException
    {
        public ReportInvalidTimeFilterException() : base("Device.ReportInvalidTimeFilter", "The time filter for the report is invalid! EndTime must be greater than StartTime", StatusCodes.Status400BadRequest)
        {
        }
    }
}
