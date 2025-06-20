using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportGetReportsConsumer(IReportService reportService, ILogger<ReportGetReportsConsumer> logger) : IConsumer<ReportGetReportsRequestMessage>
    {
        private readonly IReportService _reportService = reportService;
        private readonly ILogger<ReportGetReportsConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<ReportGetReportsRequestMessage> context)
        {
            _logger.LogDebug("Processing ReportGetReportsRequestMessage for user: {UserId}", context.Message.UserId);

            Page<List<ReportDto>> reports = await _reportService.GetReports(
                context.Message.UserId,
                context.Message.Query,
                context.Message.Page,
                context.Message.ExpandType);

            await context.RespondAsync(new ReportGetReportsResponseMessage
            {
                Reports = reports
            });
        }
    }
}
