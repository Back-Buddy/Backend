using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class ReportInvalidNameException : AbstractBaseException
    {
        public ReportInvalidNameException() : base("Report.InvalidName", "Report name is invalid.", StatusCodes.Status400BadRequest)
        {
        }
    }
}
