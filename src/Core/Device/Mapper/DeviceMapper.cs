using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Entities;

namespace BackBuddy.Core.Library.Device.Mapper
{
    public static class DeviceMapper
    {
        public static DeviceStatusDto ToDto(this DeviceStatusEntity entity, Guid deviceId)
        {
            return new DeviceStatusDto
            {
                DeviceId = deviceId,
                StartTime = entity.StartTime
            };
        }
    }
}
