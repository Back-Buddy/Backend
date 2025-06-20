using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportCreateConsumer(IReportService reportService, ILogger<ReportCreateConsumer> logger) : IConsumer<ReportCreateRequestMessage>
    {
        private readonly IReportService _reportService = reportService;
        private readonly ILogger<ReportCreateConsumer> _logger = logger;


        public async Task Consume(ConsumeContext<ReportCreateRequestMessage> context)
        {
            _logger.LogDebug("Processing ReportCreateRequestMessage for user: {UserId}", context.Message.UserId);

            ReportDto report = await _reportService.CreateReport(context.Message.UserId, context.Message.Request);

            await context.RespondAsync(new ReportCreateResponseMessage
            {
                Report = report
            });
        }
    }
}