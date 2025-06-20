using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Core.Library.Device.Enums;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportGetVisibilityTypeForUserConsumer(IReportService reportService, ILogger<ReportGetVisibilityTypeForUserConsumer> logger) : IConsumer<ReportGetVisibilityTypeForUserRequestMessage>
    {
        private readonly IReportService _reportService = reportService;
        private readonly ILogger<ReportGetVisibilityTypeForUserConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<ReportGetVisibilityTypeForUserRequestMessage> context)
        {
            _logger.LogDebug("Processing ReportGetVisibilityTypeForUserRequestMessage for user: {UserId}, report: {ReportId}",
                context.Message.UserId, context.Message.TargetReport.Id);

            IEnumerable<ReportVisibilityType> visibilityTypes = await _reportService.GetVisibilityTypeForUser(context.Message.UserId, context.Message.TargetReport);

            await context.RespondAsync(new ReportGetVisibilityTypeForUserResponseMessage
            {
                VisibilityTypes = visibilityTypes
            });
        }
    }
}