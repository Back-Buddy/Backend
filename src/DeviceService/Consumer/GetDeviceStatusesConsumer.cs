using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.WebSocket;
using BackBuddy.Device.Service.Repositories;
using MassTransit;

namespace BackBuddy.Device.Service.Consumer
{
    public class GetDeviceStatusesConsumer(IDeviceStatusRepository deviceStatusRepository) : IConsumer<GetDeviceStatusesRequestMessage>
    {
        private readonly IDeviceStatusRepository _deviceStatusRepository = deviceStatusRepository;
        public async Task Consume(ConsumeContext<GetDeviceStatusesRequestMessage> context)
        {
            IEnumerable<DeviceStatusDto> statuses = await _deviceStatusRepository.GetAllStatuses(context.CancellationToken);
            await context.RespondAsync(new GetDeviceStatusesResponseMessage
            {
                StatusEntities = statuses
            });
        }
    }
}
