using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class ReportLikeCannotLikeOwnReportException : AbstractBaseException
    {
        public ReportLikeCannotLikeOwnReportException() : base("Report.LikeCannotLikeOwn", "You can not like your own report.", StatusCodes.Status400BadRequest)
        {
        }
    }
}
