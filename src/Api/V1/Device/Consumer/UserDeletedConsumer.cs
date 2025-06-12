using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.Users.Dtos.Messages;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Device.Consumer
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
