using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer.Report
{
    public class ReportAddLikeConsumer(IReportLikeService reportLikeService, ILogger<ReportAddLikeConsumer> logger) : IConsumer<ReportAddLikeRequestMessage>
    {
        private readonly IReportLikeService _reportLikeService = reportLikeService;
        private readonly ILogger<ReportAddLikeConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<ReportAddLikeRequestMessage> context)
        {
            try
            {
                _logger.LogDebug("Processing ReportAddLikeRequestMessage for user: {UserId}, report: {ReportId}",
                    context.Message.UserId, context.Message.Report.Id);

                await _reportLikeService.AddLike(context.Message.UserId, context.Message.Report, context.Message.VisibilityTypes);
                await context.RespondAsync(new ReportAddLikeResponseMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ReportAddLikeRequestMessage");
                throw;
            }
        }
    }
}