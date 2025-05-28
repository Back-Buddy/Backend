using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.Entities;

namespace BackBuddy.Api.Service.V1.Device.Mapper
{
    public static class DeviceMapper
    {
        public async static Task<DeviceDto> ToDto(this DeviceEntity entity, Func<Guid, Task<bool>> onlineFunc)
        {
            return new DeviceDto()
            {
                Id = entity.Id,
                Name = entity.Name,
                Threshold = entity.Threshold,
                Active = entity.Active,
                Online = await onlineFunc.Invoke(entity.Id),
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

        public static ReportDto ToDto(this ReportEntity entity)
        {
            return new ReportDto()
            {
                Id = entity.Id,
                DeviceId = entity.DeviceId,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                UsedLogs = entity.UsedLogs,
                Metadata = entity.Metadata.ToDto()
            };
        }

        public static List<DeviceLogDto> ToDto(this IEnumerable<DeviceLogEntity> entities)
        {
            return [.. entities.Select(e => e.ToDto())];
        }

        public static ReportMetadataDto ToDto(this ReportMetadataEntity entity)
        {
            return new ReportMetadataDto()
            {
                TotalTime = entity.TotalTime,
                SitTime = entity.SitTime,
                StandTime = entity.StandTime,
                SitPercentage = entity.SitPercentage,
                StandPercentage = entity.StandPercentage,
                PostureChanges = entity.PostureChanges,
                AverageSitPeriod = entity.AverageSitPeriod,
                ShortestSitPeriod = entity.ShortestSitPeriod,
                LongestSitPeriod = entity.LongestSitPeriod,
            };
        }

        public async static Task<List<DeviceDto>> ToDto(this IEnumerable<DeviceEntity> entities, Func<Guid, Task<bool>> onlineFunc)
        {
            IEnumerable<Task<DeviceDto>> tasks = entities.Select(entity => entity.ToDto(onlineFunc));
            DeviceDto[] dtos = await Task.WhenAll(tasks);
            return [.. dtos];
        }

        public static List<ReportDto> ToDto(this IEnumerable<ReportEntity> entities)
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
    }
}
