using BackBuddy.Api.Service.V1.Device.DTOs.Http;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Enums;
using BackBuddy.Api.Service.V1.Utilities;
using MongoDB.Driver;

namespace BackBuddy.Api.Service.V1.Device.Repositories
{
    public interface IReportRepository
    {
        Task<ReportEntity?> Get(Guid id, CancellationToken cancellationToken = default);
        Task<Page<List<ReportEntity>>> GetAll(string userId, IEnumerable<ReportVisibilityType> visibilityTypes, ReportQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default);
        Task Add(ReportEntity entity, CancellationToken cancellationToken = default);
        Task Update(ReportEntity entity, CancellationToken cancellationToken = default);
        Task Delete(Guid id, CancellationToken cancellationToken = default);
        Task DeleteFromDevice(Guid deviceId, CancellationToken cancellationToken = default);
        Task<Page<List<ReportEntity>>> GetReportFeed(string userId, IEnumerable<string> strongRelationUser, ReportFeedQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default);
    }

    public class ReportRepository(IMongoCollection<ReportEntity> collection) : IReportRepository
    {
        private readonly IMongoCollection<ReportEntity> _collection = collection;

        public async Task Add(ReportEntity entity, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        }

        public async Task Update(ReportEntity entity, CancellationToken cancellationToken = default)
        {
            await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
        }

        public async Task Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteOneAsync(x => x.Id == id, cancellationToken: cancellationToken);
        }

        public async Task DeleteFromDevice(Guid deviceId, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteManyAsync(x => x.DeviceId == deviceId, cancellationToken: cancellationToken);
        }

        public async Task<ReportEntity?> Get(Guid id, CancellationToken cancellationToken = default)
        {
            IAsyncCursor<ReportEntity> cursor = await _collection.FindAsync(x => x.Id == id, cancellationToken: cancellationToken);
            return await cursor.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Page<List<ReportEntity>>> GetAll(string userId, IEnumerable<ReportVisibilityType> visibilityTypes, ReportQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            List<FilterDefinition<ReportEntity>> filters = [];
            filters.Add(Builders<ReportEntity>.Filter.Eq(x => x.UserId, userId));
            filters.Add(Builders<ReportEntity>.Filter.In(x => x.VisibilityType, visibilityTypes));
            if (query.Devices.Count > 0)
                filters.Add(Builders<ReportEntity>.Filter.In(x => x.DeviceId, query.Devices));
            if (query.StartTime != null)
                filters.Add(Builders<ReportEntity>.Filter.Gte(x => x.StartTime, query.StartTime.Value.ToUniversalTime()));
            if (query.EndTime != null)
                filters.Add(Builders<ReportEntity>.Filter.Lte(x => x.EndTime, query.EndTime.Value.ToUniversalTime()));

            FilterDefinition<ReportEntity> finalFilter = Builders<ReportEntity>.Filter.And(filters);

            FindOptions<ReportEntity> findOptions = new()
            {
                Skip = page.Offset(),
                Limit = page.Size,
                Sort = query.Descending ? Builders<ReportEntity>.Sort.Descending(x => x.CreatedAt) : Builders<ReportEntity>.Sort.Ascending(x => x.CreatedAt)
            };

            IAsyncCursor<ReportEntity> cursor = await _collection.FindAsync(finalFilter, findOptions, cancellationToken: cancellationToken);
            List<ReportEntity> reportEntities = await cursor.ToListAsync(cancellationToken);
            long total = await _collection.CountDocumentsAsync(finalFilter, cancellationToken: cancellationToken);
            bool hasMoreEntries = total > (page.Offset() + reportEntities.Count);

            return new Page<List<ReportEntity>>
            {
                HasMoreEntries = hasMoreEntries,
                Items = reportEntities
            };
        }

        public async Task<Page<List<ReportEntity>>> GetReportFeed(string userId, IEnumerable<string> strongRelationUser, ReportFeedQueryDto query, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            FilterDefinitionBuilder<ReportEntity> filterBuilder = Builders<ReportEntity>.Filter;

            FilterDefinition<ReportEntity> ownFilter = filterBuilder.Eq(x => x.UserId, userId);

            FilterDefinition<ReportEntity> strongRelationFilter = filterBuilder.In(x => x.UserId, strongRelationUser);
            FilterDefinition<ReportEntity> visibilityFilterRelation = filterBuilder.Eq(x => x.VisibilityType, ReportVisibilityType.Followers);

            FilterDefinition<ReportEntity> finalRelationFilter = filterBuilder.And(strongRelationFilter, visibilityFilterRelation);

            FilterDefinition<ReportEntity> visibilityFilterPublic = filterBuilder.Eq(x => x.VisibilityType, ReportVisibilityType.All);

            FilterDefinition<ReportEntity> finalFilter = filterBuilder.Or(ownFilter, finalRelationFilter, visibilityFilterPublic);

            FindOptions<ReportEntity> findOptions = new()
            {
                Skip = page.Offset(),
                Limit = page.Size,
                Sort = query.Descending ? Builders<ReportEntity>.Sort.Descending(x => x.CreatedAt) : Builders<ReportEntity>.Sort.Ascending(x => x.CreatedAt)
            };
            IAsyncCursor<ReportEntity> cursor = await _collection.FindAsync(finalFilter, findOptions, cancellationToken: cancellationToken);
            List<ReportEntity> reportEntities = await cursor.ToListAsync(cancellationToken);

            long total = await _collection.CountDocumentsAsync(finalFilter, cancellationToken: cancellationToken);
            bool hasMoreEntries = total > (page.Offset() + reportEntities.Count);

            return new Page<List<ReportEntity>>
            {
                HasMoreEntries = hasMoreEntries,
                Items = reportEntities
            };
        }
    }
}
