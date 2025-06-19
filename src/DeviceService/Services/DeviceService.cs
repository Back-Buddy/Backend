using BackBuddy.Api.Service.V1.Database.Redis;
using BackBuddy.Core.Library.Database.KeyVault;
using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Device.Dtos.WebSocket;
using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Core.Library.Device.Enums;
using BackBuddy.Core.Library.Device.Exceptions;
using BackBuddy.Core.Library.Notifications.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Core.Library.WebSockets.Dtos;
using BackBuddy.Device.Service.Entities;
using BackBuddy.Device.Service.Mapper;
using BackBuddy.Device.Service.Repositories;
using MassTransit;
using System.Text.RegularExpressions;

namespace BackBuddy.Device.Service.Services
{
    public interface IDeviceService
    {
        Task<DeviceSecretDto> Create(string userId, DeviceCreateRequestDto request, CancellationToken cancellationToken = default);
        Task Update(string userId, Guid deviceId, DeviceUpdateRequestDto request, CancellationToken cancellationToken = default);
        Task Delete(string userId, Guid deviceId, CancellationToken cancellationToken = default);
        Task DeleteAll(string userId, CancellationToken cancellationToken = default);
        Task<DeviceDto> Get(string userId, Guid deviceId, CancellationToken cancellationToken = default);
        Task<Page<List<DeviceDto>>> GetAll(string userId, PageRequestDto page, DeviceQueryDto query, CancellationToken cancellationToken = default);
        Task<DeviceDto> Authorize(string secret, CancellationToken cancellationToken = default);
        Task TryUpdateSecret(Guid deviceId, CancellationToken cancellationToken = default);
        Task AckNewSecret(Guid deviceId, string secret, CancellationToken cancellationToken = default);
        Task HandleStatusUpdate(Guid deviceId, DeviceUpdateStatusMessage status, CancellationToken cancellationToken = default);
        Task ValidateDeviceStatuses(IEnumerable<DeviceStatusDto> statusDtos, CancellationToken cancellationToken = default);
        Task<bool> IsDeviceConnected(Guid deviceId);
    }

    public partial class DeviceService(IDeviceRepository repository, IDeviceStatusRepository deviceStatusRepository, IDeviceLogRepository deviceLogRepository, IReportService reportService, ISecretProvider secretProvider, IPublisher publisher, IPublishEndpoint publishEndpoint, IRequestClient<GetFcmTokensRequestMessage> fcmTokenRequestClient, IRequestClient<WebSocketDeviceIsOnlineRequest> deviceIsOnlineRequestClient, ILogger<DeviceService> logger) : IDeviceService
    {
        private const string NAME_PATTERN = @"^[a-zA-Z0-9 \-]{3,16}$";
        private readonly static TimeSpan SECRET_EXPIRATION_TIME = TimeSpan.FromSeconds(1);

        private readonly IDeviceRepository _repository = repository;
        private readonly IDeviceStatusRepository _deviceStatusRepository = deviceStatusRepository;
        private readonly IDeviceLogRepository _deviceLogRepository = deviceLogRepository;
        private readonly IReportService _reportService = reportService;
        private readonly ISecretProvider _secretProvider = secretProvider;
        private readonly IPublisher _publisher = publisher;
        private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
        private readonly IRequestClient<GetFcmTokensRequestMessage> _fcmTokenRequestClient = fcmTokenRequestClient;
        private readonly IRequestClient<WebSocketDeviceIsOnlineRequest> _deviceIsOnlineRequestClient = deviceIsOnlineRequestClient;
        private readonly ILogger<DeviceService> _logger = logger;

        public async Task<DeviceSecretDto> Create(string userId, DeviceCreateRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!NameRegex().IsMatch(request.Name))
                throw new DeviceInvalidNameException();
            if (!await _repository.IsNameUnique(userId, request.Name, cancellationToken))
                throw new DeviceNameIsNotUniqueException();

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
            await _reportService.DeleteAllFromDeviceId(deviceId, cancellationToken);
            await _deviceStatusRepository.DeleteCurrentStatus(deviceId, cancellationToken);
            await _deviceLogRepository.DeleteLogs(deviceId, cancellationToken);
            await _repository.Delete(deviceId, cancellationToken);
        }

        public async Task DeleteAll(string userId, CancellationToken cancellationToken = default)
        {
            DeviceQueryDto query = new();
            int page = 1;

            Page<List<DeviceEntity>> devicesPage;
            List<DeviceEntity> entities = [];
            do
            {
                PageRequestDto pageRequestDto = new()
                {
                    Page = page++,
                    Size = 10000
                };
                devicesPage = await _repository.GetAll(userId, pageRequestDto, query, cancellationToken);

                entities.AddRange(devicesPage.Items);
            } while (devicesPage.HasMoreEntries && !cancellationToken.IsCancellationRequested);


            IEnumerable<Task> tasks = entities.Select(x => Delete(userId, x.Id, cancellationToken));

            await Task.WhenAll(tasks);
        }

        public async Task<DeviceDto> Get(string userId, Guid deviceId, CancellationToken cancellationToken = default)
        {
            DeviceEntity device = await _repository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();
            return await device.ToDto(IsDeviceConnected);
        }

        public async Task<Page<List<DeviceDto>>> GetAll(string userId, PageRequestDto page, DeviceQueryDto query, CancellationToken cancellationToken = default)
        {
            Page<List<DeviceEntity>> devices = await _repository.GetAll(userId, page, query, cancellationToken);
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
                    throw new DeviceInvalidNameException();
                if (!await _repository.IsNameUnique(userId, request.Name, cancellationToken))
                    throw new DeviceNameIsNotUniqueException();
                device.Name = request.Name;
                isDirty = true;
            }

            if (request.Active.HasValue && request.Active.Value != device.Active)
            {
                if (request.Active.Value)
                {
                    await _repository.DeactivateAllDevices(userId, deviceId, cancellationToken);
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

            WebSocketSendMessage webSocketMessage = new WebSocketSendMessageBuilder(deviceEntity.Id, message).Build();
            await _publisher.PublishAsync(webSocketMessage);
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
            WebSocketSendMessage webSocketMessage = new WebSocketSendMessageBuilder(deviceEntity.Id, ackMessage).Build();
            await _publisher.PublishAsync(webSocketMessage);
        }

        public async Task HandleStatusUpdate(Guid deviceId, DeviceUpdateStatusMessage status, CancellationToken cancellationToken = default)
        {
            DeviceEntity deviceEntity = await _repository.Get(deviceId, cancellationToken) ?? throw new DeviceNotFoundException();
            DeviceStatusEntity? currentDeviceStatus = await _deviceStatusRepository.GetCurrentStatus(deviceEntity.Id, cancellationToken);
            switch (status.UserPositionStatus)
            {
                case UserPositionStatusType.Sitting:
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
                case UserPositionStatusType.Standing when currentDeviceStatus != null: // Only when current status is not null => Sit Session
                    await LogDeviceSit(deviceId, currentDeviceStatus.StartTime, DateTime.UtcNow, cancellationToken);
                    await _deviceStatusRepository.DeleteCurrentStatus(deviceEntity.Id, cancellationToken);
                    break;
            }

            WebSocketSendMessage webSocketMessage = new WebSocketSendMessageBuilder(deviceEntity.Id, new DeviceUpdateStatusAckMessage()).Build();
            await _publisher.PublishAsync(webSocketMessage);
        }

        public async Task<bool> IsDeviceConnected(Guid deviceId)
        {
            Response<WebSocketDeviceIsOnlineResponse> response = await _deviceIsOnlineRequestClient.GetResponse<WebSocketDeviceIsOnlineResponse>(new WebSocketDeviceIsOnlineRequest
            {
                DeviceId = deviceId
            });
            return response.Message.IsOnline;
        }

        public async Task ValidateDeviceStatuses(IEnumerable<DeviceStatusDto> statusDtos, CancellationToken cancellationToken = default)
        {
            IEnumerable<Task> tasks = statusDtos.Select(statusEntity => ValidateDeviceStatus(statusEntity, cancellationToken));
            await Task.WhenAll(tasks);
        }

        private async Task ValidateDeviceStatus(DeviceStatusDto status, CancellationToken cancellationToken = default)
        {
            DeviceEntity? deviceEntity = await _repository.Get(status.DeviceId, cancellationToken);
            if (deviceEntity == null)
            {
                _logger.LogWarning("Device with ID {DeviceId} not found for status validation! Deleting DeviceStatus", status.DeviceId);
                await _deviceStatusRepository.DeleteCurrentStatus(status.DeviceId, cancellationToken);
                return;
            }

            DateTime? lastNotification = await _deviceStatusRepository.GetLastNotificationTime(status.DeviceId, cancellationToken);
            bool sendNotification = SendNotification(deviceEntity, status, lastNotification);
            if (!sendNotification)
                return;

            _logger.LogInformation("Device with ID {DeviceId} has status older than threshold", status.DeviceId);

            Response<GetFcmTokensResponseMessage> fcmTokenResponse = await _fcmTokenRequestClient.GetResponse<GetFcmTokensResponseMessage>(new GetFcmTokensRequestMessage
            {
                UserId = deviceEntity.UserId
            }, cancellationToken);

            (string title, string body) = GetRandomNotificationMessage(deviceEntity);

            SendNotificationRequestMessage notificationRequest = new()
            {
                Tokens = fcmTokenResponse.Message.Tokens,
                Notification = new NotificationBuilder()
                    .SetTitle(title)
                    .SetBody(body)
                    .Build()
            };
            await _publishEndpoint.Publish(notificationRequest, cancellationToken);

            await _deviceStatusRepository.SetLastNotificationTime(status.DeviceId, DateTime.UtcNow, cancellationToken);
        }

        internal static bool SendNotification(DeviceEntity deviceEntity, DeviceStatusDto statusDto, DateTime? lastNotification)
        {
            if (!deviceEntity.Active)
                return false;
            if (DateTime.UtcNow - statusDto.StartTime < deviceEntity.Threshold)
                return false;
            if (lastNotification.HasValue && DateTime.UtcNow - lastNotification.Value < deviceEntity.Threshold)
                return false;
            return true;
        }

        private async Task LogDeviceError(Guid deviceId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken)
        {
            DeviceLogEntity deviceLogEntity = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                LogType = DeviceLogType.Error,
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
                LogType = DeviceLogType.Sit,
                StartTime = startTime,
                EndTime = endTime
            };
            await _deviceLogRepository.AddLog(deviceLogEntity, cancellationToken);
        }
        private static (string Title, string Body) GetRandomNotificationMessage(DeviceEntity deviceEntity)
        {
            List<(string Title, string Body)> messages =
            [
                ("🧘 Kleine Pause gefällig?", $"Du sitzt schon eine Weile auf {deviceEntity.Name}. Zeit für einen kurzen Stretch!"),
                ("🚶 Bewegung tut gut!", $"{deviceEntity.Name} meldet: Ein paar Schritte würden dir jetzt gut tun."),
                ("🌟 Power-Up Zeit!", $"Du hast lange gesessen. Steh auf, beweg dich kurz – dein Körper wird’s dir danken!"),
                ("⏰ Aufstehen, bitte!", $"{deviceEntity.Name} erinnert dich freundlich: Ein bisschen Bewegung wäre jetzt ideal."),
                ("😄 Rückenfreundlicher Hinweis", $"Langes Sitzen auf {deviceEntity.Name} erkannt. Wie wär’s mit Dehnen oder Aufstehen?"),
                ("💡 Gesundheitstipp:", $"Schon länger auf {deviceEntity.Name}? Kurz aufstehen, tief durchatmen, weiter geht's!"),
                ("🤸 Zeit für eine Mini-Bewegungspause", $"{deviceEntity.Name} schlägt vor: Beine strecken, Schultern kreisen, durchstarten!"),
                ("📣 Dein Körper ruft!", $"Schon über {deviceEntity.Threshold.TotalMinutes:N0} Minuten auf {deviceEntity.Name}? Zeit für Bewegung."),
                ("💺 Dein Stuhl vermisst dich nicht", $"Vertrau uns – {deviceEntity.Name} kommt auch mal kurz ohne dich klar. Beweg dich!"),
                ("🎯 Mikro-Pause, große Wirkung", $"Kleine Unterbrechung, große Wirkung für deine Gesundheit. Jetzt aufstehen!")
            ];
            return messages[ThreadSafeRandom.Global.Next(messages.Count)];
        }

        private static string GetPreviewSecretName(Guid deviceId) => $"{deviceId}-preview";

        [GeneratedRegex(NAME_PATTERN)]
        private static partial Regex NameRegex();
    }
}
