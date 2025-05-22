using BackBuddy.Api.Service.V1.Database.KeyVault;
using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.DTOs.Http;
using BackBuddy.Api.Service.V1.Device.DTOs.WebSocket;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Exceptions;
using BackBuddy.Api.Service.V1.Device.Mapper;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Exceptions;
using BackBuddy.Api.Service.V1.Utilities;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using System.Text.RegularExpressions;

namespace BackBuddy.Api.Service.V1.Device.Services
{
    public interface IDeviceService
    {
        Task<DeviceSecretDto> Create(string userId, DeviceCreateRequestDto request, CancellationToken cancellationToken = default);
        Task Update(string userId, Guid deviceId, DeviceUpdateRequestDto request, CancellationToken cancellationToken = default);
        Task Delete(string userId, Guid deviceId, CancellationToken cancellationToken = default);
        Task<DeviceDto> Get(string userId, Guid deviceId, CancellationToken cancellationToken = default);
        Task<Page<List<DeviceDto>>> GetAll(string userId, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<DeviceDto> Authorize(string secret, CancellationToken cancellationToken = default);
        Task TryUpdateSecret(Guid deviceId, CancellationToken cancellationToken = default);
        Task AckNewSecret(Guid deviceId, string secret, CancellationToken cancellationToken = default);
        Task HandleStatusUpdate(Guid deviceId, DeviceUpdateStatusMessage status, CancellationToken cancellationToken = default);
        Task<bool> IsDeviceConnected(Guid deviceId);
    }

    public partial class DeviceService(IDeviceRepository repository, IDeviceStatusRepository deviceStatusRepository, IDeviceLogRepository deviceLogRepository, ISecretProvider secretProvider, IWebSocketService webSocketService) : IDeviceService
    {
        private const string NAME_PATTERN = @"^[a-zA-Z0-9 \-]{3,16}$";
        private readonly static TimeSpan SECRET_EXPIRATION_TIME = TimeSpan.FromSeconds(1);

        private readonly IDeviceRepository _repository = repository;
        private readonly IDeviceStatusRepository _deviceStatusRepository = deviceStatusRepository;
        private readonly IDeviceLogRepository _deviceLogRepository = deviceLogRepository;
        private readonly ISecretProvider _secretProvider = secretProvider;
        private readonly IWebSocketService _webSocketService = webSocketService;

        public async Task<DeviceSecretDto> Create(string userId, DeviceCreateRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!NameRegex().IsMatch(request.Name))
                throw new DeviceInvalidNameException(NAME_PATTERN);
            if (!await _repository.IsNameUnique(userId, request.Name, cancellationToken))
                throw new DeviceNameIsNotUniqueException(request.Name);

            DeviceEntity entity = new()
            {
                Id = Guid.CreateVersion7(),
                Name = request.Name,
                UserId = userId,
                SecretGeneratedAt = DateTime.UtcNow,
                Active = false
            };

            string secret = _secretProvider.GenerateSecret();
            await _secretProvider.SetSecret(entity.Id.ToString(), secret, cancellationToken);
            await _repository.Add(entity, cancellationToken);

            DeviceSecret deviceSecret = entity.ToSecret(secret);

            DeviceSecretDto deviceSecretDto = new()
            {
                DeviceId = deviceSecret.DeviceId,
                Secret = deviceSecret.Encode()
            };

            return deviceSecretDto;
        }

        public async Task Delete(string userId, Guid deviceId, CancellationToken cancellationToken = default)
        {
            DeviceEntity device = await _repository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();

            await _secretProvider.DeleteSecret(deviceId.ToString(), cancellationToken);
            await _deviceStatusRepository.DeleteCurrentStatus(deviceId, cancellationToken);
            await _deviceLogRepository.DeleteLogs(deviceId, cancellationToken);
            await _repository.Delete(deviceId, cancellationToken);
        }

        public async Task<DeviceDto> Get(string userId, Guid deviceId, CancellationToken cancellationToken = default)
        {
            DeviceEntity device = await _repository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();
            return await device.ToDto(IsDeviceConnected);
        }

        public async Task<Page<List<DeviceDto>>> GetAll(string userId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            Page<List<DeviceEntity>> devices = await _repository.GetAll(userId, page, cancellationToken);
            List<DeviceDto> deviceDtos = await devices.Items.ToDto(IsDeviceConnected);

            Page<List<DeviceDto>> deviceDtosPage = new()
            {
                Items = deviceDtos,
                HasMoreEntries = devices.HasMoreEntries
            };
            return deviceDtosPage;
        }

        public async Task Update(string userId, Guid deviceId, DeviceUpdateRequestDto request, CancellationToken cancellationToken = default)
        {
            DeviceEntity device = await _repository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();

            bool isDirty = false;

            if (request.Threshold.HasValue && request.Threshold.Value != device.Threshold)
            {
                device.Threshold = request.Threshold.Value;
                isDirty = true;
            }
            if (!string.IsNullOrEmpty(request.Name) && !string.IsNullOrWhiteSpace(request.Name) && !request.Name.Equals(device.Name, StringComparison.CurrentCultureIgnoreCase))
            {
                if (!NameRegex().IsMatch(request.Name))
                    throw new DeviceInvalidNameException(NAME_PATTERN);
                if (!await _repository.IsNameUnique(userId, request.Name, cancellationToken))
                    throw new DeviceNameIsNotUniqueException(request.Name);
                device.Name = request.Name;
                isDirty = true;
            }

            if (request.Active.HasValue && request.Active.Value != device.Active)
            {
                if (request.Active.Value)
                {
                    bool hasActiveDevice = await _repository.HasActiveDevices(userId, cancellationToken);
                    if (hasActiveDevice)
                    {
                        throw new DeviceActiveConflictException();
                    }
                }

                device.Active = request.Active.Value;
                isDirty = true;
            }

            if (isDirty)
                await _repository.Update(device, cancellationToken);
        }

        public async Task<DeviceDto> Authorize(string secret, CancellationToken cancellationToken = default)
        {
            DeviceSecret deviceSecret;
            try
            {
                deviceSecret = DeviceSecret.Decode(secret);
            }
            catch (Exception)
            {
                throw new DeviceUnauthorizedException();
            }

            DeviceEntity device = await _repository.Get(deviceSecret.DeviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            string storedSecret = await _secretProvider.GetSecret(device.Id.ToString(), cancellationToken);
            if (storedSecret != deviceSecret.Secret)
                throw new DeviceUnauthorizedException();
            return await device.ToDto(IsDeviceConnected);
        }

        public async Task TryUpdateSecret(Guid deviceId, CancellationToken cancellationToken = default)
        {
            DeviceEntity deviceEntity = await _repository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            if (DateTime.UtcNow <= deviceEntity.SecretGeneratedAt.Add(SECRET_EXPIRATION_TIME).ToUniversalTime())
                return;

            string newSecret = _secretProvider.GenerateSecret();
            await _secretProvider.SetSecret(GetPreviewSecretName(deviceId), newSecret, cancellationToken);

            DeviceSecret deviceSecret = new()
            {
                DeviceId = deviceEntity.Id,
                Secret = newSecret
            };

            DeviceNewSecretMessage message = new()
            {
                Secret = deviceSecret.Encode(),
            };
            await _webSocketService.SendMessage(deviceEntity.Id, message);
        }

        public async Task AckNewSecret(Guid deviceId, string secret, CancellationToken cancellationToken = default)
        {
            DeviceEntity deviceEntity = await _repository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();

            DeviceSecret deviceSecret = DeviceSecret.Decode(secret);
            string previewSecret = await _secretProvider.GetSecret(GetPreviewSecretName(deviceId), cancellationToken);

            if (previewSecret != deviceSecret.Secret)
                throw new DeviceNewSecretConflictException();

            deviceEntity.SecretGeneratedAt = DateTime.UtcNow;
            await _repository.Update(deviceEntity, cancellationToken);
            await _secretProvider.SetSecret(deviceEntity.Id.ToString(), deviceSecret.Secret, cancellationToken);

            DeviceNewSecretSetAckMessage ackMessage = new() { Secret = deviceSecret.Secret };
            await _webSocketService.SendMessage(deviceId, ackMessage);
        }

        public async Task HandleStatusUpdate(Guid deviceId, DeviceUpdateStatusMessage status, CancellationToken cancellationToken = default)
        {
            DeviceEntity deviceEntity = await _repository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            DeviceStatusEntity? currentDeviceStatus = await _deviceStatusRepository.GetCurrentStatus(deviceEntity.Id, cancellationToken);
            switch (status.UserPositionStatus)
            {
                case Enums.UserPositionStatusType.Sitting:
                    // Error because always sitting status
                    if (currentDeviceStatus != null)
                    {
                        await LogDeviceError(deviceId, currentDeviceStatus.StartTime, DateTime.UtcNow, cancellationToken);
                        await _deviceStatusRepository.DeleteCurrentStatus(deviceEntity.Id, cancellationToken);
                    }

                    DeviceStatusEntity deviceStatus = new()
                    {
                        PushSent = false,
                        StartTime = DateTime.UtcNow
                    };
                    await _deviceStatusRepository.SetCurrentStatus(deviceEntity.Id, deviceStatus, cancellationToken);
                    break;
                case Enums.UserPositionStatusType.Standing when currentDeviceStatus != null: // Only when current status is not null => Sit Session
                    await LogDeviceSit(deviceId, currentDeviceStatus.StartTime, DateTime.UtcNow, cancellationToken);
                    await _deviceStatusRepository.DeleteCurrentStatus(deviceEntity.Id, cancellationToken);
                    break;
            }

            await _webSocketService.SendMessage(deviceId, new DeviceUpdateStatusAckMessage());
        }

        public async Task<bool> IsDeviceConnected(Guid deviceId)
        {
            return await _webSocketService.IsDeviceConnected(deviceId);
        }

        private async Task LogDeviceError(Guid deviceId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken)
        {
            DeviceLogEntity deviceLogEntity = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                LogType = Enums.DeviceLogType.Error,
                StartTime = startTime,
                EndTime = endTime
            };
            await _deviceLogRepository.AddLog(deviceLogEntity, cancellationToken);
        }

        private async Task LogDeviceSit(Guid deviceId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken)
        {
            DeviceLogEntity deviceLogEntity = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                LogType = Enums.DeviceLogType.Sit,
                StartTime = startTime,
                EndTime = endTime
            };
            await _deviceLogRepository.AddLog(deviceLogEntity, cancellationToken);
        }

        private static string GetPreviewSecretName(Guid deviceId) => $"{deviceId}-preview";

        [GeneratedRegex(NAME_PATTERN)]
        private static partial Regex NameRegex();
    }
}
