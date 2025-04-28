using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.Entities;

namespace BackBuddy.Api.Service.V1.Device.Mapper
{
    public static class DeviceEntityMapper
    {

        public static DeviceSecret ToSecret(this DeviceEntity entity)
        {
            return new DeviceSecret()
            {
                DeviceId = entity.Id,
                Secret = entity.Secret
            };
        }

        public static DeviceDto ToDto(this DeviceEntity entity)
        {
            return new DeviceDto()
            {
                Id = entity.Id,
                Name = entity.Name,
                Threshold = entity.Threshold,
            };
        }

        public static List<DeviceDto> ToDto(this IEnumerable<DeviceEntity> entities)
        {
            return entities.Select(e => e.ToDto()).ToList();
        }
    }
}
