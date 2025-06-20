using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Device.Exceptions;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Device.Service.Entities;
using BackBuddy.Device.Service.Mapper;
using BackBuddy.Device.Service.Repositories;

namespace BackBuddy.Device.Service.Services
{
    public interface IDeviceLogService
    {
        Task<DeviceLogDto> GetDeviceLog(string userId, Guid deviceId, Guid logId, CancellationToken cancellationToken = default);
        Task<Page<List<DeviceLogDto>>> GetDeviceLogs(string userId, Guid deviceId, DeviceLogQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default);
    }

    public class DeviceLogService(IDeviceRepository deviceRepository, IDeviceLogRepository deviceLogRepository) : IDeviceLogService
    {
        private readonly IDeviceLogRepository _deviceLogRepository = deviceLogRepository;
        private readonly IDeviceRepository _deviceRepository = deviceRepository;

        public async Task<DeviceLogDto> GetDeviceLog(string userId, Guid deviceId, Guid logId, CancellationToken cancellationToken = default)
        {
            DeviceEntity deviceEntity = await _deviceRepository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            if (deviceEntity.UserId != userId)
                throw new DeviceUnauthorizedException();

            DeviceLogEntity deviceLogEntity = await _deviceLogRepository.GetLog(logId, cancellationToken) ?? throw new DeviceLogNotFoundException();
            if (deviceEntity.Id != deviceLogEntity.DeviceId)
                throw new DeviceLogNotFromDeviceException();

            return deviceLogEntity.ToDto();
        }

        public async Task<Page<List<DeviceLogDto>>> GetDeviceLogs(string userId, Guid deviceId, DeviceLogQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            DeviceEntity deviceEntity = await _deviceRepository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            if (deviceEntity.UserId != userId)
                throw new DeviceUnauthorizedException();

            Page<List<DeviceLogEntity>> deviceLogs = await _deviceLogRepository.GetLogs(deviceId, query, page, cancellationToken);
            Page<List<DeviceLogDto>> response = new() { Items = deviceLogs.Items.ToDto(), HasMoreEntries = deviceLogs.HasMoreEntries };

            return response;
        }
    }
}
