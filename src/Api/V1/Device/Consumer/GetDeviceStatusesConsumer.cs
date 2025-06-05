using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Core.Library.Device.Dtos;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Device.Consumer
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
