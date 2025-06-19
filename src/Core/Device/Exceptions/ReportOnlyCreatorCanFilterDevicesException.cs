using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class ReportOnlyCreatorCanFilterDevicesException : AbstractBaseException
    {
        public ReportOnlyCreatorCanFilterDevicesException() : base("Report.OnlyCreatorCanFilterDevices", "Only the creator of the report can filter devices.", StatusCodes.Status403Forbidden)
        {
        }
    }
}
