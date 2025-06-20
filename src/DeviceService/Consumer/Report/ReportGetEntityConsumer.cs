using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportGetEntityConsumer(IReportService reportService, ILogger<ReportGetEntityConsumer> logger) : IConsumer<ReportGetEntityRequestMessage>
    {
        private readonly IReportService _reportService = reportService;
        private readonly ILogger<ReportGetEntityConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<ReportGetEntityRequestMessage> context)
        {
            _logger.LogDebug("Processing ReportGetEntityRequestMessage for report: {ReportId}", context.Message.ReportId);

            ReportEntity reportEntity = await _reportService.GetReportEntity(context.Message.ReportId);

            await context.RespondAsync(new ReportGetEntityResponseMessage
            {
                Report = reportEntity
            });
        }
    }
}