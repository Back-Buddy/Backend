using BackBuddy.Api.Service.V1.Device.DTOs.Http;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Utilities;
using MongoDB.Driver;

namespace BackBuddy.Api.Service.V1.Device.Repositories
{
    public interface IDeviceLogRepository
    {
        Task AddLog(DeviceLogEntity logEntity, CancellationToken cancellationToken = default);
        Task<DeviceLogEntity?> GetLog(Guid logId, CancellationToken cancellationToken = default);
        Task<Page<List<DeviceLogEntity>>> GetLogs(Guid deviceId, DeviceLogQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default);
        Task DeleteLog(Guid logId, CancellationToken cancellationToken = default);
        Task DeleteLogs(Guid deviceId, CancellationToken cancellationToken = default);
    }

    public class DeviceLogRepository(IMongoCollection<DeviceLogEntity> collection) : IDeviceLogRepository
    {
        private readonly IMongoCollection<DeviceLogEntity> _collection = collection;

        public async Task AddLog(DeviceLogEntity logEntity, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(logEntity, cancellationToken: cancellationToken);
        }

        public async Task DeleteLog(Guid logId, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteOneAsync(x => x.Id == logId, cancellationToken: cancellationToken);
        }

        public async Task DeleteLogs(Guid deviceId, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteManyAsync(x => x.DeviceId == deviceId, cancellationToken: cancellationToken);
        }

        public async Task<DeviceLogEntity?> GetLog(Guid logId, CancellationToken cancellationToken = default)
        {
            IAsyncCursor<DeviceLogEntity> cursor = await _collection.FindAsync(x => x.Id == logId, cancellationToken: cancellationToken);
            return await cursor.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Page<List<DeviceLogEntity>>> GetLogs(Guid deviceId, DeviceLogQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            List<FilterDefinition<DeviceLogEntity>> filters = [];
            filters.Add(Builders<DeviceLogEntity>.Filter.Eq(x => x.DeviceId, deviceId));
            if (query.LogType != null)
                filters.Add(Builders<DeviceLogEntity>.Filter.Eq(x => x.LogType, query.LogType));
            if (query.StartTime != null)
                filters.Add(Builders<DeviceLogEntity>.Filter.Gte(x => x.StartTime, query.StartTime));
            if (query.EndTime != null)
                filters.Add(Builders<DeviceLogEntity>.Filter.Lte(x => x.EndTime, query.EndTime));

            FilterDefinition<DeviceLogEntity> finalFilter = Builders<DeviceLogEntity>.Filter.And(filters);

            FindOptions<DeviceLogEntity> findOptions = new()
            {
                Limit = page.Size,
                Skip = page.Offset()
            };
            IAsyncCursor<DeviceLogEntity> cursor = await _collection.FindAsync(finalFilter, findOptions, cancellationToken: cancellationToken);
            List<DeviceLogEntity> entities = await cursor.ToListAsync(cancellationToken);
            long total = await _collection.CountDocumentsAsync(finalFilter, cancellationToken: cancellationToken);

            bool hasMoreEntries = total > (page.Offset() + entities.Count);

            return new Page<List<DeviceLogEntity>>
            {
                Items = entities,
                HasMoreEntries = hasMoreEntries
            };
        }
    }
}
