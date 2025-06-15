using BackBuddy.Api.Service.V1.Exceptions;

namespace BackBuddy.Api.Service.V1.Device.Exceptions
{
    public class ReportLikeAlreadyLikedException : AbstractBaseException
    {
        public ReportLikeAlreadyLikedException() : base("Report.LikeAlreadyLiked", "You have already liked this report.", StatusCodes.Status400BadRequest)
        {
        }
    }
}