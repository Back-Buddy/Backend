using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Core.Library.Device.Enums;
using BackBuddy.Core.Library.Device.Exceptions;
using BackBuddy.Core.Library.ExceptionHandlers;
using BackBuddy.Core.Library.Exceptions;
using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Device.Service.Entities;
using BackBuddy.Device.Service.Mapper;
using BackBuddy.Device.Service.Repositories;
using MassTransit;
using System.Text.RegularExpressions;

namespace BackBuddy.Device.Service.Services
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

    public partial class ReportService(IReportLikeService reportLikeService, IDeviceLogRepository deviceLogRepository, IDeviceRepository deviceRepository, IReportRepository reportRepository, IRequestClient<GetUserRequestMessage> getUserRequestClient, IRequestClient<GetStrongFollowRelationsAndAllFollowingsRequestMessage> relationRequestClient, IRequestClient<HasUserStrongRelationRequestMessage> strongRelationRequestClient, ILogger<ReportService> logger) : IReportService
    {
        private readonly IDeviceRepository _deviceRepository = deviceRepository;
        private readonly IDeviceLogRepository _deviceLogRepository = deviceLogRepository;
        private readonly IReportRepository _reportRepository = reportRepository;
        private readonly IRequestClient<GetStrongFollowRelationsAndAllFollowingsRequestMessage> _relationRequestClient = relationRequestClient;
        private readonly IRequestClient<HasUserStrongRelationRequestMessage> _strongRelationRequestClient = strongRelationRequestClient;
        private readonly IReportLikeService _reportLikeService = reportLikeService;
        private readonly IRequestClient<GetUserRequestMessage> _getUserRequestClient = getUserRequestClient;
        private readonly ILogger<ReportService> _logger = logger;

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
            return reportEntity.ToDto(true, 0, false);
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
            if (expandType == ReportExpandType.All || expandType == ReportExpandType.DeviceLogs)
            {
                logs = await GetDeviceLogDtosFromReport(report);
            }

            UserDto? creator = null;
            if (expandType == ReportExpandType.All || expandType == ReportExpandType.Creator)
            {
                creator = await GetUserDtoFromReport(report);
            }

            long reportLikeCount = await _reportLikeService.CountLikesFromReport(reportId, cancellationToken);
            bool hasLiked = await _reportLikeService.HasLikedReport(userId, reportId, cancellationToken);
            return report.ToDto(report.UserId == userId, reportLikeCount, hasLiked, logs, creator);
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

            bool exportLogs = expandType == ReportExpandType.All || expandType == ReportExpandType.DeviceLogs;
            bool exportCreator = expandType == ReportExpandType.All || expandType == ReportExpandType.Creator;

            Page<List<ReportDto>> response = new()
            {
                Items = await reports.Items.ToDto(x => x.UserId == userId, x => _reportLikeService.CountLikesFromReport(x.Id, cancellationToken), HasUserLikedReport, userId, getDeviceLogsFunction: exportLogs ? GetDeviceLogDtosFromReport : null, getUserFunction: exportCreator ? GetUserDtoFromReport : null),
                HasMoreEntries = reports.HasMoreEntries
            };
            return response;
        }

        public async Task<Page<List<ReportDto>>> GetReportFeed(string userId, ReportFeedQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            Response<GetStrongFollowRelationsAndAllFollowingsResponseMessage> relationsAndFollowing = await _relationRequestClient.GetResponse<GetStrongFollowRelationsAndAllFollowingsResponseMessage>(new GetStrongFollowRelationsAndAllFollowingsRequestMessage { UserId = userId }, cancellationToken: cancellationToken); // Wait for the response to ensure we have the relations before proceeding
            IEnumerable<string> strongRelations = relationsAndFollowing.Message.StrongRelations;
            IEnumerable<string> following = relationsAndFollowing.Message.Following;

            Page<List<ReportEntity>> reports = await _reportRepository.GetReportFeed(userId, strongRelations, following, query, page, cancellationToken);

            bool exportLogs = query.ExpandType == ReportExpandType.All || query.ExpandType == ReportExpandType.DeviceLogs;
            bool exportCreator = query.ExpandType == ReportExpandType.All || query.ExpandType == ReportExpandType.Creator;

            Page<List<ReportDto>> response = new()
            {
                Items = await reports.Items.ToDto(x => x.UserId == userId, x => _reportLikeService.CountLikesFromReport(x.Id, cancellationToken), HasUserLikedReport, userId, exportLogs ? GetDeviceLogDtosFromReport : null, exportCreator ? GetUserDtoFromReport : null),
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
            IEnumerable<DeviceLogEntity> sitLogs = logs.Where(log => log.LogType == DeviceLogType.Sit).Where(x => x.StartTime >= startTime && x.EndTime <= endTime);

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

        private async Task<List<DeviceLogDto>> GetDeviceLogDtosFromReport(ReportEntity report)
        {
            IEnumerable<Task<DeviceLogEntity>> deviceLogTasks = report.UsedLogs.Select(x => _deviceLogRepository.GetLog(x)).OfType<Task<DeviceLogEntity>>();
            DeviceLogEntity[] logs = await Task.WhenAll(deviceLogTasks);
            return logs.ToDto();
        }

        private async Task<bool> HasUserLikedReport(ReportEntity reportEntity, string userId)
        {
            if (reportEntity.UserId == userId)
                return false; // User cannot like their own report
            return await _reportLikeService.HasLikedReport(userId, reportEntity.Id);
        }

        private async Task<UserDto?> GetUserDtoFromReport(ReportEntity report)
        {
            try
            {
                Response<GetUserResponseMessage> response = await _getUserRequestClient.GetResponse<GetUserResponseMessage>(new GetUserRequestMessage() { UserId = report.UserId });
                return response.Message.User;
            }
            catch (RequestFaultException ex)
            {
                AbstractBaseException? abstractBaseException = ex.GetAbstractBaseException();
                if (abstractBaseException != null)
                    _logger.LogError(abstractBaseException, "Failed to get user for report {ReportId} with userId {UserId}", report.Id, report.UserId);
                else
                    _logger.LogError(ex, "Failed to get user for report {ReportId} with userId {UserId}", report.Id, report.UserId);
                return null;
            }
        }

        internal async Task<IEnumerable<ReportVisibilityType>> GetReportVisibilityTypeForUser(string creatorId, string userId)
        {
            if (creatorId == userId)
                return [ReportVisibilityType.All, ReportVisibilityType.Followers, ReportVisibilityType.Private];

            bool hasStrongRelation = await HasStrongRelation(userId, creatorId);
            if (hasStrongRelation)
                return [ReportVisibilityType.All, ReportVisibilityType.Followers];
            return [ReportVisibilityType.All];
        }

        internal async Task<bool> HasStrongRelation(string userId, string targetUserId)
        {
            Response<HasUserStrongRelationResponseMessage> response = await _strongRelationRequestClient.GetResponse<HasUserStrongRelationResponseMessage>(new HasUserStrongRelationRequestMessage { UserId = userId, TargetUserId = targetUserId });
            return response.Message.HasStrongRelation;
        }

        [GeneratedRegex(@"^[a-zA-Z0-9 \-]{3,128}$")]
        private static partial Regex NameRegex();
    }
}