using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.Entities;

namespace BackBuddy.Api.Service.V1.Device.Services
{
    public interface IDeviceService
    {
        Task<DeviceSecretDto> Create(string userId, DeviceCreateRequestDto request);
        Task Update(string userId, Guid deviceId, DeviceUpdateRequestDto request);
        Task Delete(string userId, Guid deviceId);
        Task<DeviceDto> Get(string userId, Guid deviceId);
        Task<List<DeviceDto>> GetAll(string userId);
        Task<DeviceEntity> Authorize(string secret);
        //Task HandleStatusUpdate()
    }
}
