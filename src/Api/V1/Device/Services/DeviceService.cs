using BackBuddy.Api.Service.V1.Database.KeyVault;
using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.DTOs.Http;
using BackBuddy.Api.Service.V1.Device.DTOs.WebSocket;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Exceptions;
using BackBuddy.Api.Service.V1.Device.Mapper;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Utilities;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BackBuddy.Api.Service.V1.Device.Services
{
    public interface IDeviceService
    {
        Task<DeviceSecretDto> Create(string userId, DeviceCreateRequestDto request);
        Task Update(string userId, Guid deviceId, DeviceUpdateRequestDto request);
        Task Delete(string userId, Guid deviceId);
        Task<DeviceDto> Get(string userId, Guid deviceId);
        Task<Page<List<DeviceDto>>> GetAll(string userId, PageRequestDto page);
        Task<DeviceDto> Authorize(string secret);
        Task TryUpdateSecret(Guid deviceId);
        Task AckNewSecret(Guid deviceId, string secret);
        //Task HandleStatusUpdate()
    }

    public partial class DeviceService(IDeviceRepository repository, ISecretProvider secretProvider, IWebSocketService webSocketService) : IDeviceService
    {
        private const string NAME_PATTERN = @"^[a-zA-Z0-9 \-]{3,16}$";
        private readonly static TimeSpan SECRET_EXPIRATION_TIME = TimeSpan.FromSeconds(1);

        private readonly IDeviceRepository _repository = repository;
        private readonly ISecretProvider _secretProvider = secretProvider;
        private readonly IWebSocketService _webSocketService = webSocketService;

        public async Task<DeviceSecretDto> Create(string userId, DeviceCreateRequestDto request)
        {
            if (!NameRegex().IsMatch(request.Name))
                throw new DeviceInvalidNameException(NAME_PATTERN);
            if (!await _repository.IsNameUnique(userId, request.Name))
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
            await _secretProvider.SetSecret(entity.Id.ToString(), secret);
            await _repository.Add(entity);

            DeviceSecret deviceSecret = entity.ToSecret(secret);

            DeviceSecretDto deviceSecretDto = new()
            {
                DeviceId = deviceSecret.DeviceId,
                Secret = deviceSecret.Encode()
            };

            return deviceSecretDto;
        }

        public async Task Delete(string userId, Guid deviceId)
        {
            DeviceEntity device = await _repository.Get(deviceId) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();

            await _repository.Delete(deviceId);
            await _secretProvider.DeleteSecret(deviceId.ToString());
        }

        public async Task<DeviceDto> Get(string userId, Guid deviceId)
        {
            DeviceEntity device = await _repository.Get(deviceId) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();
            return device.ToDto();
        }

        public async Task<Page<List<DeviceDto>>> GetAll(string userId, PageRequestDto page)
        {
            Page<List<DeviceEntity>> devices = await _repository.GetAll(userId, page);
            List<DeviceDto> deviceDtos = devices.Items.ToDto();

            Page<List<DeviceDto>> deviceDtosPage = new()
            {
                Items = deviceDtos,
                HasMoreEntries = devices.HasMoreEntries
            };
            return deviceDtosPage;
        }

        public async Task Update(string userId, Guid deviceId, DeviceUpdateRequestDto request)
        {
            DeviceEntity device = await _repository.Get(deviceId) ?? throw new DeviceNotFoundException();
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
                if (!await _repository.IsNameUnique(userId, request.Name))
                    throw new DeviceNameIsNotUniqueException(request.Name);
                device.Name = request.Name;
                isDirty = true;
            }

            if (request.Active.HasValue && request.Active.Value != device.Active)
            {
                if (request.Active.Value)
                {
                    bool hasActiveDevice = await _repository.HasActiveDevices(userId);
                    if (hasActiveDevice)
                    {
                        throw new DeviceActiveConflictException();
                    }
                }

                device.Active = request.Active.Value;
                isDirty = true;
            }

            if (isDirty)
                await _repository.Update(device);
        }

        public async Task<DeviceDto> Authorize(string secret)
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

            DeviceEntity device = await _repository.Get(deviceSecret.DeviceId) ?? throw new DeviceNotFoundException();
            string storedSecret = await _secretProvider.GetSecret(device.Id.ToString());
            if (storedSecret != deviceSecret.Secret)
                throw new DeviceUnauthorizedException();
            return device.ToDto();
        }

        public async Task TryUpdateSecret(Guid deviceId)
        {
            DeviceEntity deviceEntity = await _repository.Get(deviceId) ?? throw new DeviceNotFoundException();
            if (DateTime.UtcNow <= deviceEntity.SecretGeneratedAt.Add(SECRET_EXPIRATION_TIME).ToUniversalTime())
                return;

            string newSecret = _secretProvider.GenerateSecret();
            await _secretProvider.SetSecret(GetPreviewSecretName(deviceId), newSecret);

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

        public async Task AckNewSecret(Guid deviceId, string secret)
        {
            DeviceEntity deviceEntity = await _repository.Get(deviceId) ?? throw new DeviceNotFoundException();

            DeviceSecret deviceSecret = DeviceSecret.Decode(secret);
            string previewSecret = await _secretProvider.GetSecret(GetPreviewSecretName(deviceId));

            if (previewSecret != deviceSecret.Secret)
                throw new DeviceNewSecretConflictException();

            deviceEntity.SecretGeneratedAt = DateTime.UtcNow;
            await _repository.Update(deviceEntity);
            await _secretProvider.SetSecret(deviceEntity.Id.ToString(), deviceSecret.Secret);
        
            DeviceNewSecretSetAckMessage ackMessage = new() { Secret = deviceSecret.Secret };
            await _webSocketService.SendMessage(deviceId, ackMessage);
        }

        private static string GetPreviewSecretName(Guid deviceId) => $"{deviceId.ToString()}-preview";

        [GeneratedRegex(NAME_PATTERN)]
        private static partial Regex NameRegex();
    }
}
