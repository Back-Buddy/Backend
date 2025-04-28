using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Utilities;

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
}
