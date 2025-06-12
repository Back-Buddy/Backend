using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class ReportInvalidNameException : AbstractBaseException
    {
        public ReportInvalidNameException() : base("Report.InvalidName", "Report name is invalid.", StatusCodes.Status400BadRequest)
        {
        }
    }
}
