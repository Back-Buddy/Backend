using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.Entities;

namespace BackBuddy.Api.Service.V1.Device.Mapper
{
    public static class DeviceMapper
    {
        public static DeviceDto ToDto(this DeviceEntity entity)
        {
            return new DeviceDto()
            {
                Id = entity.Id,
                Name = entity.Name,
                Threshold = entity.Threshold,
                Active = entity.Active
            };
        }
        public static List<DeviceDto> ToDto(this IEnumerable<DeviceEntity> entities)
        {
            return [.. entities.Select(e => e.ToDto())];
        }

        public static DeviceSecret ToSecret(this DeviceEntity entity, string secret)
        {
            return new DeviceSecret()
            {
                DeviceId = entity.Id,
                Secret = secret,
            };
        }

        public static DeviceLogDto ToDto(this DeviceLogEntity entity)
        {
            return new DeviceLogDto()
            {
                Id = entity.Id,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                LogType = entity.LogType,
            };
        }

        public static List<DeviceLogDto> ToDto(this IEnumerable<DeviceLogEntity> entities)
        {
            return [.. entities.Select(e => e.ToDto())];
        }
    }
}
