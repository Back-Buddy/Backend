using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class ReportLikeCannotLikeOwnReportException : AbstractBaseException
    {
        public ReportLikeCannotLikeOwnReportException() : base("Report.LikeCannotLikeOwn", "You can not like your own report.", StatusCodes.Status400BadRequest)
        {
        }
    }
}
