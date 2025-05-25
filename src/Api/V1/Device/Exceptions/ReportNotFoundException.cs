using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class ReportNotFoundException : AbstractBaseException
    {
        public ReportNotFoundException() : base("Device.ReportNotFound", "The report was not found", StatusCodes.Status404NotFound)
        {
        }
    }
}
