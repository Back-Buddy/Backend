using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

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

        public async Task<Page<List<DeviceEntity>>> GetAll(string userId, PageRequestDto page)
        {
            FilterDefinition<DeviceEntity> filter = Builders<DeviceEntity>.Filter
                .Eq(x => x.UserId, userId);
            FindOptions<DeviceEntity> findOptions = new()
            {
                Limit = page.Size,
                Skip = page.Offset()
            };
            IAsyncCursor<DeviceEntity> cursor = await collection.FindAsync(filter, findOptions);
            long total = await collection.CountDocumentsAsync(filter);

            List<DeviceEntity> deviceEntities = await cursor.ToListAsync();
            bool hasMoreEntries = total > (page.Offset() + deviceEntities.Count);

            Page<List<DeviceEntity>> result = new()
            {
                Items = deviceEntities,
                HasMoreEntries = hasMoreEntries
            };
            return result;
        }

        public async Task<bool> IsNameUnique(string userId, string name)
        {
            FilterDefinition<DeviceEntity> filterDefinition = Builders<DeviceEntity>.Filter
                .And(
                    Builders<DeviceEntity>.Filter.Eq(x => x.UserId, userId),
                    Builders<DeviceEntity>.Filter.Regex(u => u.Name, new BsonRegularExpression(Regex.Escape(name), "i"))
                );
            IAsyncCursor<DeviceEntity> cursor = await collection.FindAsync(filterDefinition);
            return !(await cursor.AnyAsync());
        }

        public async Task Update(DeviceEntity entity)
        {
            await collection.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        }
    }
}
