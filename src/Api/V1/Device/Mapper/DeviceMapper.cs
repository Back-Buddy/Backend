using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.WebSockets.Services;

namespace BackBuddy.Api.Service.V1.Device.Mapper
{
    public static class DeviceMapper
    {
        public async static Task<DeviceDto> ToDto(this DeviceEntity entity, IWebSocketService webSocketService)
        {
            return new DeviceDto()
            {
                Id = entity.Id,
                Name = entity.Name,
                Threshold = entity.Threshold,
                Active = entity.Active,
                Online = await webSocketService.IsDeviceConnected(entity.Id),
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

        public async static Task<List<DeviceDto>> ToDto(this IEnumerable<DeviceEntity> entities, IWebSocketService webSocketService)
        {
            List<DeviceDto> dtos = [];
            foreach (DeviceEntity entity in entities)
            {
                dtos.Add(await entity.ToDto(webSocketService));
            }
            return dtos;
        }

        public static DeviceSecret ToSecret(this DeviceEntity entity, string secret)
        {
            return new DeviceSecret()
            {
                DeviceId = entity.Id,
                Secret = secret,
            };
        }
    }
}
