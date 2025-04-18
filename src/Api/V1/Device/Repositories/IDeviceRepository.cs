using BackBuddy.Api.Service.V1.Device.Entities;

namespace BackBuddy.Api.Service.V1.Device.Repositories
{
    public interface IDeviceRepository
    {
        Task Add(DeviceEntity entity);
        Task Update(DeviceEntity entity);
        Task Delete(Guid id);
        Task<DeviceEntity?> Get(Guid id);
    }
}
