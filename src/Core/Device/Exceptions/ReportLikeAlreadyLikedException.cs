using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BackBuddy.Core.Library.Device.Exceptions
{
    public class ReportLikeAlreadyLikedException : AbstractBaseException
    {
        public ReportLikeAlreadyLikedException() : base("Report.LikeAlreadyLiked", "You have already liked this report.", StatusCodes.Status400BadRequest)
        {
        }
    }
}