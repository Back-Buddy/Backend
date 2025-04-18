using BackBuddy.Api.Service.V1.Device.Entities;
using MongoDB.Driver;

namespace BackBuddy.Api.Service.V1.Device.Repositories
{
    public class DeviceRepository(IMongoCollection<DeviceEntity> collection) : IDeviceRepository
    {
        public async Task Add(DeviceEntity entity)
        {
            await collection.InsertOneAsync(entity);
        }

        public async Task Delete(Guid id)
        {
            await collection.DeleteOneAsync(x => x.Id == id);
        }

        public async Task<DeviceEntity?> Get(Guid id)
        {
            IAsyncCursor<DeviceEntity> cursor = await collection.FindAsync(x => x.Id == id);
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task Update(DeviceEntity entity)
        {
            await collection.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        }
    }
}
