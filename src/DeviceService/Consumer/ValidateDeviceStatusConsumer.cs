using BackBuddy.Core.Library.Device.Dtos.WebSocket;
using BackBuddy.Device.Service.Services;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer
{
    public class ValidateDeviceStatusConsumer(IDeviceService deviceService) : IConsumer<ValidateDeviceStatusRequestMessage>
    {
        private readonly IDeviceService _deviceService = deviceService;

        public async Task Consume(ConsumeContext<ValidateDeviceStatusRequestMessage> context)
        {
            await _deviceService.ValidateDeviceStatuses(context.Message.StatusEntities);
            await context.RespondAsync(new ValidateDeviceStatusResponseMessage());
        }
    }
}
