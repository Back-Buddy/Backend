using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.DTOs.Http;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Enums;
using BackBuddy.Api.Service.V1.Device.Exceptions;
using BackBuddy.Api.Service.V1.Device.Mapper;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Users.Services;
using BackBuddy.Api.Service.V1.Utilities;
using System.Text.RegularExpressions;

namespace BackBuddy.Api.Service.V1.Device.Services
{
    public interface IReportService
    {
        Task<ReportDto> CreateReport(string userId, ReportCreateDto request, CancellationToken cancellationToken = default);
        Task UpdateReport(string userId, Guid reportId, ReportUpdateDto request, CancellationToken cancellationToken = default);
        Task<ReportDto> GetReport(string userId, Guid reportId, ReportExpandType expandType, CancellationToken cancellationToken = default);
        Task<ReportEntity> GetReportEntity(Guid reportId, CancellationToken cancellationToken = default);
        Task<Page<List<ReportDto>>> GetReports(string userId, ReportQueryDto query, PageRequestDto page, ReportExpandType expandType, CancellationToken cancellationToken = default);
        Task DeleteReport(string userId, Guid reportId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Deletes all reports from a device. This will also delete all likes from the reports. *Important*: No permission check is done here, so this should only be used by the device service when deleting a device.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAllFromDeviceId(Guid deviceId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ReportVisibilityType>> GetVisibilityTypeForUser(string userId, ReportEntity targetReport);
        Task<IEnumerable<ReportVisibilityType>> GetVisibilityTypeForUser(string userId, Guid targetReport, CancellationToken cancellationToken = default);
        Task<Page<List<ReportDto>>> GetReportFeed(string userId, ReportFeedQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default);
    }

    public partial class ReportService(IReportLikeService reportLikeService, IUserRelationService relationService, IDeviceLogRepository deviceLogRepository, IDeviceRepository deviceRepository, IReportRepository reportRepository) : IReportService
    {
        private readonly IDeviceRepository _deviceRepository = deviceRepository;
        private readonly IDeviceLogRepository _deviceLogRepository = deviceLogRepository;
        private readonly IReportRepository _reportRepository = reportRepository;
        private readonly IUserRelationService _relationService = relationService;
        private readonly IReportLikeService _reportLikeService = reportLikeService;

        public async Task<ReportDto> CreateReport(string userId, ReportCreateDto request, CancellationToken cancellationToken = default)
        {
            if (!NameRegex().IsMatch(request.Name))
                throw new ReportInvalidNameException();

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
                LogType = DeviceLogType.Sit,
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
                Name = request.Name,
                VisibilityType = request.VisibilityType,
                UserId = userId,
                DeviceId = request.DeviceId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Metadata = reportMetadata,
                UsedLogs = [.. usedLogs.Select(log => log.Id)],
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.Add(reportEntity, cancellationToken);
            return reportEntity.ToDto(true, 0);
        }

        public async Task UpdateReport(string userId, Guid reportId, ReportUpdateDto request, CancellationToken cancellationToken = default)
        {
            ReportEntity report = await _reportRepository.Get(reportId, cancellationToken) ?? throw new ReportNotFoundException();
            if (report.UserId != userId)
                throw new DeviceUserForbiddenException();

            if (!string.IsNullOrEmpty(request.Name) && request.Name != report.Name)
            {
                if (!NameRegex().IsMatch(request.Name))
                    throw new ReportInvalidNameException();
                report.Name = request.Name;
            }

            if (request.VisibilityType != null && request.VisibilityType != report.VisibilityType)
            {
                report.VisibilityType = request.VisibilityType.Value;
            }

            await _reportRepository.Update(report, cancellationToken);
        }

        public async Task DeleteReport(string userId, Guid reportId, CancellationToken cancellationToken = default)
        {
            ReportEntity report = await _reportRepository.Get(reportId, cancellationToken) ?? throw new ReportNotFoundException();
            if (report.UserId != userId)
                throw new DeviceUserForbiddenException();
            await _reportLikeService.DeleteAllLikesFromReport(reportId, cancellationToken);
            await _reportRepository.Delete(reportId, cancellationToken);
        }

        public async Task DeleteAllFromDeviceId(Guid deviceId, CancellationToken cancellationToken = default)
        {
            IEnumerable<ReportEntity> reports = await _reportRepository.GetAllFromDevice(deviceId, cancellationToken);

            IEnumerable<Task> tasks = reports.Select(async report =>
            {
                await _reportLikeService.DeleteAllLikesFromReport(report.Id, cancellationToken);
                await _reportRepository.Delete(report.Id, cancellationToken);
            });
            await Task.WhenAll(tasks);
        }

        public async Task<ReportDto> GetReport(string userId, Guid reportId, ReportExpandType expandType, CancellationToken cancellationToken = default)
        {
            ReportEntity report = await _reportRepository.Get(reportId, cancellationToken) ?? throw new ReportNotFoundException();

            IEnumerable<ReportVisibilityType> visibilityTypes = await GetReportVisibilityTypeForUser(report.UserId, userId);
            if (!visibilityTypes.Contains(report.VisibilityType))
                throw new ReportNotFoundException(); // User does not have access to this report (e.g., private report)

            List<DeviceLogDto>? logs = null;
            if (expandType == ReportExpandType.DeviceLogs)
            {
                logs = await GetDeviceLogDtos(report);
            }

            long reportLikeCount = await _reportLikeService.CountLikesFromReport(reportId, cancellationToken);
            return report.ToDto(report.UserId == userId, reportLikeCount, logs);
        }

        public async Task<ReportEntity> GetReportEntity(Guid reportId, CancellationToken cancellationToken = default)
        {
            ReportEntity report = await _reportRepository.Get(reportId, cancellationToken) ?? throw new ReportNotFoundException();
            return report;
        }

        public async Task<Page<List<ReportDto>>> GetReports(string userId, ReportQueryDto query, PageRequestDto page, ReportExpandType expandType, CancellationToken cancellationToken = default)
        {
            string targetUserId = query.UserId ?? userId; // default to the current user if no user is specified

            IEnumerable<ReportVisibilityType> visibilityTypes = await GetReportVisibilityTypeForUser(userId, targetUserId);
            if (userId != targetUserId && query.Devices.Count != 0)
                throw new ReportOnlyCreatorCanFilterDevicesException(); // Only the creator of the report can filter by devices

            Page<List<ReportEntity>> reports = await _reportRepository.GetAll(targetUserId, visibilityTypes, query, page, cancellationToken);
            Page<List<ReportDto>> response = new()
            {
                Items = await reports.Items.ToDto(x => x.UserId == userId, x => _reportLikeService.CountLikesFromReport(x.Id, cancellationToken), expandType == ReportExpandType.DeviceLogs ? GetDeviceLogDtos : null),
                HasMoreEntries = reports.HasMoreEntries
            };
            return response;
        }

        private async Task<List<DeviceLogDto>> GetDeviceLogDtos(ReportEntity report)
        {
            IEnumerable<Task<DeviceLogEntity>> deviceLogTasks = report.UsedLogs.Select(x => _deviceLogRepository.GetLog(x)).OfType<Task<DeviceLogEntity>>();
            DeviceLogEntity[] logs = await Task.WhenAll(deviceLogTasks);
            return logs.ToDto();
        }

        public async Task<Page<List<ReportDto>>> GetReportFeed(string userId, ReportFeedQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            (IEnumerable<string> strongRelations, IEnumerable<string> following) = await _relationService.GetStrongFollowRelationsAndAllFollowings(userId, cancellationToken);
            Page<List<ReportEntity>> reports = await _reportRepository.GetReportFeed(userId, strongRelations, following, query, page, cancellationToken);
            Page<List<ReportDto>> response = new()
            {
                Items = await reports.Items.ToDto(x => x.UserId == userId, x => _reportLikeService.CountLikesFromReport(x.Id, cancellationToken), query.ExpandType == ReportExpandType.DeviceLogs ? GetDeviceLogDtos : null),
                HasMoreEntries = reports.HasMoreEntries
            };
            return response;
        }

        internal static (ReportMetadataEntity MetaData, IEnumerable<DeviceLogEntity> UsedLogs) AnalyzeLogs(List<DeviceLogEntity> logs, DateTime startTime, DateTime endTime)
        {
            startTime = startTime.ToUniversalTime();
            endTime = endTime.ToUniversalTime();

            if (startTime > DateTime.UtcNow.AddMinutes(5)) // Start time is in the future, which is invalid (5 minutes buffer to allow for clock skew)
                throw new ReportInvalidTimeFilterException();

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

        public async Task<IEnumerable<ReportVisibilityType>> GetVisibilityTypeForUser(string userId, ReportEntity targetReport)
        {
            return await GetReportVisibilityTypeForUser(targetReport.UserId, userId);
        }

        public async Task<IEnumerable<ReportVisibilityType>> GetVisibilityTypeForUser(string userId, Guid targetReport, CancellationToken cancellationToken = default)
        {
            ReportEntity report = await _reportRepository.Get(targetReport, cancellationToken) ?? throw new ReportNotFoundException();
            return await GetReportVisibilityTypeForUser(report.UserId, userId);
        }

        internal async Task<IEnumerable<ReportVisibilityType>> GetReportVisibilityTypeForUser(string creatorId, string userId)
        {
            if (creatorId == userId)
                return [ReportVisibilityType.All, ReportVisibilityType.Followers, ReportVisibilityType.Private];

            bool hasStrongRelation = await _relationService.HasStrongRelation(userId, creatorId);
            if (hasStrongRelation)
                return [ReportVisibilityType.All, ReportVisibilityType.Followers];
            return [ReportVisibilityType.All];
        }

        [GeneratedRegex(@"^[a-zA-Z0-9 \-]{3,128}$")]
        private static partial Regex NameRegex();
    }
}