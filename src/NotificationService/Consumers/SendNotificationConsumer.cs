using BackBuddy.Core.Library.Notifications.Dtos;
using BackBuddy.Notification.Service.Services;
using MassTransit;

namespace BackBuddy.Notification.Service.Consumers
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
