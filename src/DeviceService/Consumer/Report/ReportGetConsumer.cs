using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportGetConsumer(IReportService reportService, ILogger<ReportGetConsumer> logger) : IConsumer<ReportGetRequestMessage>
    {
        private readonly IReportService _reportService = reportService;
        private readonly ILogger<ReportGetConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<ReportGetRequestMessage> context)
        {
            _logger.LogDebug("Processing ReportGetRequestMessage for user: {UserId}, report: {ReportId}",
                context.Message.UserId, context.Message.ReportId);

            ReportDto reportDto = await _reportService.GetReport(context.Message.UserId, context.Message.ReportId, context.Message.ExpandType);

            await context.RespondAsync(new ReportGetResponseMessage
            {
                Report = reportDto
            });
        }
    }
}