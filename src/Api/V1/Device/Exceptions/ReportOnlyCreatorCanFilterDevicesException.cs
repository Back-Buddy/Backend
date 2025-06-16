using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class ReportOnlyCreatorCanFilterDevicesException : AbstractBaseException
    {
        public ReportOnlyCreatorCanFilterDevicesException() : base("Report.OnlyCreatorCanFilterDevices", "Only the creator of the report can filter devices.", StatusCodes.Status403Forbidden)
        {
        }
    }
}
