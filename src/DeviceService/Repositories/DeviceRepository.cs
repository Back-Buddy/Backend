using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Device.Service.Entities;
using BackBuddy.Core.Library.Device.Dtos.Http;

namespace BackBuddy.Device.Service.Repositories
{
    public interface IDeviceRepository
    {
        Task Add(DeviceEntity entity, CancellationToken cancellationToken = default);
        Task Update(DeviceEntity entity, CancellationToken cancellationToken = default);
        Task Delete(Guid id, CancellationToken cancellationToken = default);
        Task<DeviceEntity?> Get(Guid id, CancellationToken cancellationToken = default);
        Task<Page<List<DeviceEntity>>> GetAll(string userId, PageRequestDto page, DeviceQueryDto query, CancellationToken cancellationToken = default);
        Task<bool> IsNameUnique(string userId, string name, CancellationToken cancellationToken = default);
        Task<bool> HasActiveDevices(string userId, CancellationToken cancellationToken = default);
        Task DeactivateAllDevices(string userId, Guid excludeDeviceId, CancellationToken cancellationToken = default);
    }

    public class DeviceRepository(IMongoCollection<DeviceEntity> collection) : IDeviceRepository
    {
        private readonly IMongoCollection<DeviceEntity> _collection = collection;
        public async Task Add(DeviceEntity entity, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        }

        public async Task Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<DeviceEntity?> Get(Guid id, CancellationToken cancellationToken = default)
        {
            IAsyncCursor<DeviceEntity> cursor = await _collection.FindAsync(x => x.Id == id, cancellationToken: cancellationToken);
            return await cursor.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Page<List<DeviceEntity>>> GetAll(string userId, PageRequestDto page, DeviceQueryDto query, CancellationToken cancellationToken = default)
        {
            List<FilterDefinition<DeviceEntity>> filters = [];
            filters.Add(Builders<DeviceEntity>.Filter.Eq(x => x.UserId, userId));
            
            if (query.Active.HasValue)
                filters.Add(Builders<DeviceEntity>.Filter.Eq(x => x.Active, query.Active.Value));

            FilterDefinition<DeviceEntity> finalFilter = Builders<DeviceEntity>.Filter.And(filters);

            SortDefinition<DeviceEntity> sortDefinition = query.Descending
                ? Builders<DeviceEntity>.Sort.Descending(x => x.Name)
                : Builders<DeviceEntity>.Sort.Ascending(x => x.Name);

            FindOptions<DeviceEntity> findOptions = new()
            {
                Limit = page.Size,
                Skip = page.Offset(),
                Sort = sortDefinition
            };

            IAsyncCursor<DeviceEntity> cursor = await _collection.FindAsync(finalFilter, findOptions, cancellationToken: cancellationToken);
            long total = await _collection.CountDocumentsAsync(finalFilter, cancellationToken: cancellationToken);

            List<DeviceEntity> deviceEntities = await cursor.ToListAsync(cancellationToken);
            bool hasMoreEntries = total > page.Offset() + deviceEntities.Count;

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
            IAsyncCursor<DeviceEntity> cursor = await _collection.FindAsync(filterDefinition, cancellationToken: cancellationToken);
            return !await cursor.AnyAsync(cancellationToken);
        }

        public async Task Update(DeviceEntity entity, CancellationToken cancellationToken = default)
        {
            await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
        }

        public async Task<bool> HasActiveDevices(string userId, CancellationToken cancellationToken = default)
        {
            FilterDefinition<DeviceEntity> filter = Builders<DeviceEntity>.Filter
                .And(
                    Builders<DeviceEntity>.Filter.Eq(x => x.UserId, userId),
                    Builders<DeviceEntity>.Filter.Eq(x => x.Active, true)
                );
            IAsyncCursor<DeviceEntity> cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
            return await cursor.AnyAsync(cancellationToken);
        }

        public async Task DeactivateAllDevices(string userId, Guid excludeDeviceId, CancellationToken cancellationToken = default)
        {
            List<DeviceEntity> activeDevices = await GetActiveDevices(userId, excludeDeviceId, cancellationToken);

            IEnumerable<Task<ReplaceOneResult>> tasks = activeDevices.Select(device => 
            {
                device.Active = false;
                return _collection.ReplaceOneAsync(
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
            
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }
    }
}
