using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportGetLikesFromReportConsumer(IReportLikeService reportLikeService, ILogger<ReportGetLikesFromReportConsumer> logger) : IConsumer<ReportGetLikesFromReportRequestMessage>
    {
        private readonly IReportLikeService _reportLikeService = reportLikeService;
        private readonly ILogger<ReportGetLikesFromReportConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<ReportGetLikesFromReportRequestMessage> context)
        {
            _logger.LogDebug("Processing ReportGetLikesFromReportRequestMessage for report: {ReportId}", context.Message.Report.Id);

            Page<List<string>> likes = await _reportLikeService.GetReportLikesFromReport(context.Message.Report, context.Message.VisibilityTypes, context.Message.Page);

            await context.RespondAsync(new ReportGetLikesFromReportResponseMessage
            {
                Likes = likes
            });
        }
    }
}
