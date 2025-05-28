using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.DTOs.Http;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Exceptions;
using BackBuddy.Api.Service.V1.Device.Mapper;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Utilities;

namespace BackBuddy.Api.Service.V1.Device.Services
{
    public interface IReportService
    {
        Task<ReportDto> CreateReport(string userId, ReportCreateDto request, CancellationToken cancellationToken = default);
        Task<ReportDto> GetReport(string userId, Guid reportId, CancellationToken cancellationToken = default);
        Task<Page<List<ReportDto>>> GetReports(string userId, ReportQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default);
        Task DeleteReport(string userId, Guid reportId, CancellationToken cancellationToken = default);
    }

    public class ReportService(IDeviceLogRepository deviceLogRepository, IDeviceRepository deviceRepository, IReportRepository reportRepository) : IReportService
    {
        private readonly IDeviceRepository _deviceRepository = deviceRepository;
        private readonly IDeviceLogRepository _deviceLogRepository = deviceLogRepository;
        private readonly IReportRepository _reportRepository = reportRepository;

        public async Task<ReportDto> CreateReport(string userId, ReportCreateDto request, CancellationToken cancellationToken = default)
        {
            DeviceEntity device = await _deviceRepository.Get(request.DeviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUserForbiddenException();
            if (request.StartTime > request.EndTime)
                throw new ReportInvalidTimeFilterException();

            // Get Logs in Time-Range
            DeviceLogQueryDto queryDto = new()
            {
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                LogType = Enums.DeviceLogType.Sit,
                Descending = false
            };
            List<DeviceLogEntity> logs = [];
            Page<List<DeviceLogEntity>> result;
            int page = 1;
            do
            {
                result = await _deviceLogRepository.GetLogs(request.DeviceId, queryDto, new()
                {
                    Size = 10000,
                    Page = page++
                }, cancellationToken);
                logs.AddRange(result.Items);
            } while (result.HasMoreEntries && !cancellationToken.IsCancellationRequested);

            // Analayze the logs
            (ReportMetadataEntity reportMetadata, IEnumerable<DeviceLogEntity> usedLogs) = AnalyzeLogs(logs, request.StartTime, request.EndTime);

            // Create Report
            ReportEntity reportEntity = new()
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                DeviceId = request.DeviceId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Metadata = reportMetadata,
                UsedLogs = [.. usedLogs.Select(log => log.Id)]
            };

            await _reportRepository.Add(reportEntity, cancellationToken);
            return reportEntity.ToDto();
        }

        public async Task DeleteReport(string userId, Guid reportId, CancellationToken cancellationToken = default)
        {
            ReportEntity report = await _reportRepository.Get(reportId, cancellationToken) ?? throw new ReportNotFoundException();
            if (report.UserId != userId)
                throw new DeviceUserForbiddenException();
            await _reportRepository.Delete(reportId, cancellationToken);
        }
        public async Task<ReportDto> GetReport(string userId, Guid reportId, CancellationToken cancellationToken = default)
        {
            ReportEntity report = await _reportRepository.Get(reportId, cancellationToken) ?? throw new ReportNotFoundException();
            if (report.UserId != userId)
                throw new DeviceUserForbiddenException();
            return report.ToDto();
        }

        public async Task<Page<List<ReportDto>>> GetReports(string userId, ReportQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            Page<List<ReportEntity>> reports = await _reportRepository.GetAll(userId, query, page, cancellationToken);
            Page<List<ReportDto>> response = new()
            {
                Items = reports.Items.ToDto(),
                HasMoreEntries = reports.HasMoreEntries
            };
            return response;
        }

        internal static (ReportMetadataEntity MetaData, IEnumerable<DeviceLogEntity> UsedLogs) AnalyzeLogs(List<DeviceLogEntity> logs, DateTime startTime, DateTime endTime)
        {
            TimeSpan totalTime = endTime - startTime;
            if (totalTime <= TimeSpan.Zero)
                throw new ReportInvalidTimeFilterException();
            IEnumerable<DeviceLogEntity> sitLogs = logs.Where(log => log.LogType == Enums.DeviceLogType.Sit).Where(x => x.StartTime >= startTime && x.EndTime <= endTime);

            TimeSpan allSitTime = TimeSpan.FromTicks(0);
            TimeSpan? longestSitPeriod = null;
            TimeSpan? shortestSitPeriod = null;
            foreach (DeviceLogEntity log in sitLogs)
            {
                TimeSpan sitTime = log.EndTime - log.StartTime;
                allSitTime += sitTime;

                if (longestSitPeriod == null || longestSitPeriod < sitTime)
                    longestSitPeriod = sitTime;
                if (shortestSitPeriod == null || shortestSitPeriod > sitTime)
                    shortestSitPeriod = sitTime;
            }

            TimeSpan standTime = totalTime - allSitTime;
            TimeSpan? averageSitPeriod = sitLogs.Any() ? TimeSpan.FromTicks(allSitTime.Ticks / sitLogs.Count()) : null;

            return (new ReportMetadataEntity
            {
                TotalTime = totalTime,
                SitTime = allSitTime,
                StandTime = standTime,
                SitPercentage = Math.Round(allSitTime.TotalMicroseconds / totalTime.TotalMicroseconds, 4) * 100,
                StandPercentage = Math.Round(standTime.TotalMicroseconds / totalTime.TotalMicroseconds, 4) * 100,
                PostureChanges = sitLogs.Count(),
                LongestSitPeriod = longestSitPeriod,
                ShortestSitPeriod = shortestSitPeriod,
                AverageSitPeriod = averageSitPeriod
            }, sitLogs);
        }
    }
}