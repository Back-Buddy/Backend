using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Mapper;
using BackBuddy.Api.Service.V1.Device.Repositories;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BackBuddy.Api.Service.V1.Device.Services
{
    public partial class DeviceService(IDeviceRepository repository) : IDeviceService
    {
        private const string NAME_PATTERN = @"^[a-zA-Z0-9]{3,16}$";

        public async Task<DeviceSecretDto> Create(string userId, DeviceCreateRequestDto request)
        {
            if (!NameRegex().IsMatch(request.Name))
                throw new DeviceInvalidNameException(NAME_PATTERN);

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
                Secret = deviceSecret.Encode()
            };

            return deviceSecretDto;
        }

        public async Task Delete(string userId, Guid deviceId)
        {
            DeviceEntity device = repository.Get(deviceId) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();
            await repository.Delete(deviceId);
        }

        public async Task<DeviceDto> Get(string userId, Guid deviceId)
        {
            DeviceEntity device = repository.Get(deviceId) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();
            return device.ToDto();
        }

        public Task<List<DeviceDto>> GetAll(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task Update(string userId, Guid deviceId, DeviceUpdateRequestDto request)
        {
            DeviceEntity device = repository.Get(deviceId) ?? throw new DeviceNotFoundException();
            if (device.UserId != userId)
                throw new DeviceUnauthorizedException();
        }

        public Task<DeviceEntity> Authorize(string secret)
        {
            throw new NotImplementedException();
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
