using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Utilities;

namespace BackBuddy.Api.Service.V1.Device.Repositories
{
    public interface IDeviceRepository
    {
        Task Add(DeviceEntity entity);
        Task Update(DeviceEntity entity);
        Task Delete(Guid id);
        Task<DeviceEntity?> Get(Guid id);
        Task<Page<List<DeviceEntity>>> GetAll(string userId, PageRequestDto page);
        Task<bool> IsNameUnique(string userId, string name);
    }
}
