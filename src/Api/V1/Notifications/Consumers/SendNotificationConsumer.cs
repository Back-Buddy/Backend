using BackBuddy.Api.Service.V1.Notifications.Dtos;
using BackBuddy.Api.Service.V1.Notifications.Services;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Notifications.Consumers
{
    public class SendNotificationConsumer(INotificationService notificationService) : IConsumer<SendNotificationRequestMessage>
    {
        private readonly INotificationService _notificationService = notificationService;

        public async Task Consume(ConsumeContext<SendNotificationRequestMessage> context)
        {
            await _notificationService.SendNotification(context.Message.Tokens, context.Message.Notification, context.CancellationToken);
        }
    }
}
