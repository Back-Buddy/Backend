using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Core.Library.Device.Dtos;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Device.Consumer
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
