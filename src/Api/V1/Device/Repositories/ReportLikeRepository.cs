using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Utilities;
using MongoDB.Driver;

namespace BackBuddy.Api.Service.V1.Device.Repositories
{
    public interface IReportLikeRepository
    {
        Task AddLike(ReportLikeEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAllLikesFromUser(string userId, CancellationToken cancellationToken = default);
        Task DeleteAllLikesFromReport(Guid reportId, CancellationToken cancellationToken = default);
        Task DeleteLike(string userId, Guid reportId, CancellationToken cancellationToken = default);
        Task<bool> HasLikedReport(string userId, Guid reportId, CancellationToken cancellationToken = default);
        Task<Page<List<ReportLikeEntity>>> GetReportLikesFromUser(string userid, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<Page<List<ReportLikeEntity>>> GetReportLikesFromReport(Guid reportId, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<long> CountLikesFromUser(string userId, CancellationToken cancellationToken = default);
        Task<long> CountLikesFromReport(Guid reportId, CancellationToken cancellationToken = default);
    }

    public class ReportLikeRepository(IMongoCollection<ReportLikeEntity> collection) : IReportLikeRepository
    {
        private readonly IMongoCollection<ReportLikeEntity> _collection = collection;

        public async Task AddLike(ReportLikeEntity entity, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        }

        public async Task<long> CountLikesFromReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            FilterDefinition<ReportLikeEntity> filter = new FilterDefinitionBuilder<ReportLikeEntity>().Eq(x => x.ReportId, reportId);
            long count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            return count;
        }

        public async Task<long> CountLikesFromUser(string userId, CancellationToken cancellationToken = default)
        {
            FilterDefinition<ReportLikeEntity> filter = new FilterDefinitionBuilder<ReportLikeEntity>().Eq(x => x.UserId, userId);
            long count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            return count;
        }

        public async Task DeleteAllLikesFromReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteManyAsync(x => x.ReportId == reportId, cancellationToken);
        }

        public async Task DeleteAllLikesFromUser(string userId, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteManyAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task DeleteLike(string userId, Guid reportId, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteOneAsync(x => x.UserId == userId && x.ReportId == reportId, cancellationToken);
        }

        public async Task<Page<List<ReportLikeEntity>>> GetReportLikesFromReport(Guid reportId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            FindOptions<ReportLikeEntity> options = new()
            {
                Limit = page.Size,
                Skip = page.Offset()
            };
            FilterDefinition<ReportLikeEntity> filter = Builders<ReportLikeEntity>.Filter.Eq(x => x.ReportId, reportId);

            IAsyncCursor<ReportLikeEntity> cursor = await _collection.FindAsync(filter, options, cancellationToken);
            List<ReportLikeEntity> likes = await cursor.ToListAsync(cancellationToken);
            long documentCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            bool hasMoreEntries = documentCount > (page.Offset() + likes.Count);
            return new Page<List<ReportLikeEntity>>
            {
                Items = likes,
                HasMoreEntries = hasMoreEntries
            };
        }

        public async Task<Page<List<ReportLikeEntity>>> GetReportLikesFromUser(string userid, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            FindOptions<ReportLikeEntity> options = new()
            {
                Limit = page.Size,
                Skip = page.Offset()
            };
            FilterDefinition<ReportLikeEntity> filter = Builders<ReportLikeEntity>.Filter.Eq(x => x.UserId, userid);

            IAsyncCursor<ReportLikeEntity> cursor = await _collection.FindAsync(filter, options, cancellationToken);
            List<ReportLikeEntity> likes = await cursor.ToListAsync(cancellationToken);
            long documentCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            bool hasMoreEntries = documentCount > (page.Offset() + likes.Count);
            return new Page<List<ReportLikeEntity>>
            {
                Items = likes,
                HasMoreEntries = hasMoreEntries
            };
        }

        public async Task<bool> HasLikedReport(string userId, Guid reportId, CancellationToken cancellationToken = default)
        {
            IAsyncCursor<ReportLikeEntity> cursor = await _collection.FindAsync(x => x.UserId == userId && x.ReportId == reportId, cancellationToken: cancellationToken);
            return await cursor.AnyAsync(cancellationToken);
        }
    }
}
