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
    public partial class DeviceService(IDeviceRepository repository) : IDeviceService
    {
        private const string NAME_PATTERN = @"^[a-zA-Z0-9 \-]{3,16}$";

        public async Task<DeviceSecretDto> Create(string userId, DeviceCreateRequestDto request)
        {
            if (!NameRegex().IsMatch(request.Name))
                throw new DeviceInvalidNameException(NAME_PATTERN);
            if (!await repository.IsNameUnique(userId, request.Name))
                throw new DeviceNameIsNotUniqueException(request.Name);

            DeviceEntity entity = new()
            {
                Id = Guid.CreateVersion7(),
                Name = request.Name,
                UserId = userId,
                Secret = GenerateSecret()
            };

            await repository.Add(entity);

            DeviceSecret deviceSecret = entity.ToSecret();
            DeviceSecretDto deviceSecretDto = new()
            {
                DeviceId = deviceSecret.DeviceId,
                Secret = deviceSecret.Encode()
            };

            return deviceSecretDto;
        }

        public async Task Delete(string userId, Guid deviceId)
        {
            DeviceEntity device = await repository.Get(deviceId) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();
            await repository.Delete(deviceId);
        }

        public async Task<DeviceDto> Get(string userId, Guid deviceId)
        {
            DeviceEntity device = await repository.Get(deviceId) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();
            return device.ToDto();
        }

        public async Task<Page<List<DeviceDto>>> GetAll(string userId, PageRequestDto page)
        {
            Page<List<DeviceEntity>> devices = await repository.GetAll(userId, page);
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
            DeviceEntity device = await repository.Get(deviceId) ?? throw new DeviceNotFoundException();
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
                if (!await repository.IsNameUnique(userId, request.Name))
                    throw new DeviceNameIsNotUniqueException(request.Name);
                device.Name = request.Name;
                isDirty = true;
            }

            if (isDirty)
                await repository.Update(device);
        }

        public async Task<DeviceEntity> Authorize(string secret)
        {
            DeviceSecret deviceSecret;
            try
            {
                deviceSecret = JsonSerializer.Deserialize<DeviceSecret>(Convert.FromBase64String(secret)) ?? throw new JsonException();
            }
            catch (Exception)
            {
                throw new DeviceUnauthorizedException();
            }
            DeviceEntity device = await repository.Get(deviceSecret.DeviceId) ?? throw new DeviceNotFoundException();
            if(device.Secret != deviceSecret.Secret)
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
