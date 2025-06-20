using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportDeleteConsumer(IReportService reportService, ILogger<ReportDeleteConsumer> logger) : IConsumer<ReportDeleteRequestMessage>
    {
        private readonly IReportService _reportService = reportService;
        private readonly ILogger<ReportDeleteConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<ReportDeleteRequestMessage> context)
        {
            _logger.LogDebug("Processing ReportDeleteRequestMessage for user: {UserId}, report: {ReportId}",
                context.Message.UserId, context.Message.ReportId);

            await _reportService.DeleteReport(context.Message.UserId, context.Message.ReportId);

            await context.RespondAsync(new ReportDeleteResponseMessage());
        }
    }
}