using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Device.Service.Services;
using MassTransit;
namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportUpdateConsumer(IReportService reportService, ILogger<ReportUpdateConsumer> logger) : IConsumer<ReportUpdateRequestMessage>
    {
        private readonly IReportService _reportService = reportService;
        private readonly ILogger<ReportUpdateConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<ReportUpdateRequestMessage> context)
        {
            _logger.LogDebug("Processing ReportUpdateRequestMessage for user: {UserId}, report: {ReportId}",
                context.Message.UserId, context.Message.ReportId);

            await _reportService.UpdateReport(context.Message.UserId, context.Message.ReportId, context.Message.Request);

            await context.RespondAsync(new ReportUpdateResponseMessage());
        }
    }
}
