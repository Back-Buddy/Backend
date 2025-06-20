using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportGetFeedConsumer(IReportService reportService, ILogger<ReportGetFeedConsumer> logger) : IConsumer<ReportGetFeedRequestMessage>
    {
        private readonly IReportService _reportService = reportService;
        private readonly ILogger<ReportGetFeedConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<ReportGetFeedRequestMessage> context)
        {
            _logger.LogDebug("Processing ReportGetFeedRequestMessage for user: {UserId}", context.Message.UserId);

            Page<List<ReportDto>> reports = await _reportService.GetReportFeed(context.Message.UserId, context.Message.Query, context.Message.Page);

            await context.RespondAsync(new ReportGetFeedResponseMessage
            {
                Reports = reports
            });
        }
    }
}