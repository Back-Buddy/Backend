using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer
{
    public class UserDeletedConsumer(IDeviceService deviceService) : IConsumer<UserDeletedMessage>
    {
        private readonly IDeviceService _deviceService = deviceService;

        public async Task Consume(ConsumeContext<UserDeletedMessage> context)
        {
            await _deviceService.DeleteAll(context.Message.UserId);
        }
    }
}
