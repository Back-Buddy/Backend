using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace BackBuddy.Api.Service.V1.Device.Repositories
{
    public interface IDeviceRepository
    {
        Task Add(DeviceEntity entity, CancellationToken cancellationToken = default);
        Task Update(DeviceEntity entity, CancellationToken cancellationToken = default);
        Task Delete(Guid id, CancellationToken cancellationToken = default);
        Task<DeviceEntity?> Get(Guid id, CancellationToken cancellationToken = default);
        Task<Page<List<DeviceEntity>>> GetAll(string userId, PageRequestDto page, bool? active = null, CancellationToken cancellationToken = default);
        Task<bool> IsNameUnique(string userId, string name, CancellationToken cancellationToken = default);
        Task<bool> HasActiveDevices(string userId, CancellationToken cancellationToken = default);
        Task DeactivateAllDevices(string userId, Guid excludeDeviceId, CancellationToken cancellationToken = default);
    }

    public class DeviceRepository(IMongoCollection<DeviceEntity> collection) : IDeviceRepository
    {
        public async Task Add(DeviceEntity entity, CancellationToken cancellationToken = default)
        {
            await collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        }

        public async Task Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<DeviceEntity?> Get(Guid id, CancellationToken cancellationToken = default)
        {
            IAsyncCursor<DeviceEntity> cursor = await collection.FindAsync(x => x.Id == id, cancellationToken: cancellationToken);
            return await cursor.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Page<List<DeviceEntity>>> GetAll(string userId, PageRequestDto page, bool? active = null, CancellationToken cancellationToken = default)
        {
            List<FilterDefinition<DeviceEntity>> filters = [];
            filters.Add(Builders<DeviceEntity>.Filter.Eq(x => x.UserId, userId));
            
            if (active.HasValue)
                filters.Add(Builders<DeviceEntity>.Filter.Eq(x => x.Active, active.Value));

            FilterDefinition<DeviceEntity> finalFilter = Builders<DeviceEntity>.Filter.And(filters);

            FindOptions<DeviceEntity> findOptions = new()
            {
                Limit = page.Size,
                Skip = page.Offset()
            };
            IAsyncCursor<DeviceEntity> cursor = await collection.FindAsync(finalFilter, findOptions, cancellationToken: cancellationToken);
            long total = await collection.CountDocumentsAsync(finalFilter, cancellationToken: cancellationToken);

            List<DeviceEntity> deviceEntities = await cursor.ToListAsync(cancellationToken);
            bool hasMoreEntries = total > (page.Offset() + deviceEntities.Count);

            Page<List<DeviceEntity>> result = new()
            {
                Items = deviceEntities,
                HasMoreEntries = hasMoreEntries
            };
            return result;
        }

        public async Task<bool> IsNameUnique(string userId, string name, CancellationToken cancellationToken = default)
        {
            FilterDefinition<DeviceEntity> filterDefinition = Builders<DeviceEntity>.Filter
                .And(
                    Builders<DeviceEntity>.Filter.Eq(x => x.UserId, userId),
                    Builders<DeviceEntity>.Filter.Regex(u => u.Name, new BsonRegularExpression(Regex.Escape(name), "i"))
                );
            IAsyncCursor<DeviceEntity> cursor = await collection.FindAsync(filterDefinition, cancellationToken: cancellationToken);
            return !(await cursor.AnyAsync(cancellationToken));
        }

        public async Task Update(DeviceEntity entity, CancellationToken cancellationToken = default)
        {
            await collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
        }

        public async Task<bool> HasActiveDevices(string userId, CancellationToken cancellationToken = default)
        {
            FilterDefinition<DeviceEntity> filter = Builders<DeviceEntity>.Filter
                .And(
                    Builders<DeviceEntity>.Filter.Eq(x => x.UserId, userId),
                    Builders<DeviceEntity>.Filter.Eq(x => x.Active, true)
                );
            IAsyncCursor<DeviceEntity> cursor = await collection.FindAsync(filter, cancellationToken: cancellationToken);
            return await cursor.AnyAsync(cancellationToken);
        }

        public async Task DeactivateAllDevices(string userId, Guid excludeDeviceId, CancellationToken cancellationToken = default)
        {
            List<DeviceEntity> activeDevices = await GetActiveDevices(userId, excludeDeviceId, cancellationToken);

            IEnumerable<Task<ReplaceOneResult>> tasks = activeDevices.Select(device => 
            {
                device.Active = false;
                return collection.ReplaceOneAsync(
                    d => d.Id == device.Id, 
                    device, 
                    cancellationToken: cancellationToken);
            });

            await Task.WhenAll(tasks);
        }
        
        private async Task<List<DeviceEntity>> GetActiveDevices(string userId, Guid excludeDeviceId, CancellationToken cancellationToken = default)
        {
            FilterDefinition<DeviceEntity> filter = Builders<DeviceEntity>.Filter
                .And(
                    Builders<DeviceEntity>.Filter.Eq(x => x.UserId, userId),
                    Builders<DeviceEntity>.Filter.Ne(x => x.Id, excludeDeviceId),
                    Builders<DeviceEntity>.Filter.Eq(x => x.Active, true)
                );
            
            return await collection.Find(filter).ToListAsync(cancellationToken);
        }
    }
}
