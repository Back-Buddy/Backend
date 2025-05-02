using BackBuddy.Api.Service.V1.Database.KeyVault;
using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Exceptions;
using BackBuddy.Api.Service.V1.Device.Mapper;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Utilities;
using System.Security.Cryptography;
using System.Text.Json;
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
        Task<DeviceEntity> Authorize(string secret);
        //Task HandleStatusUpdate()
    }

    public partial class DeviceService(IDeviceRepository repository, ISecretProvider secretProvider) : IDeviceService
    {
        private const string NAME_PATTERN = @"^[a-zA-Z0-9 \-]{3,16}$";

        private readonly IDeviceRepository _repository = repository;
        private readonly ISecretProvider _secretProvider = secretProvider;

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
            };

            string secret = GenerateSecret();
            await _secretProvider.SetSecret(entity.Id.ToString(), secret);
            await _repository.Add(entity);

            string storedSecret = await _secretProvider.GetSecret(entity.Id.ToString());
            DeviceSecret deviceSecret = entity.ToSecret(storedSecret);

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

            if (isDirty)
                await _repository.Update(device);
        }

        public async Task<DeviceEntity> Authorize(string secret)
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
            return device;
        }

        private static string GenerateSecret()
        {
            byte[] randomBytes = new byte[256];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        [GeneratedRegex(NAME_PATTERN)]
        private static partial Regex NameRegex();
    }
}
