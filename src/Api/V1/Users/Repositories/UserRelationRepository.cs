using BackBuddy.Api.Service.V1.Users.Entities;
using BackBuddy.Api.Service.V1.Utilities;
using MongoDB.Driver;

namespace BackBuddy.Api.Service.V1.Users.Repositories
{

    public interface IUserRelationRepository
    {
        Task Add(UserFollowEntity userFollowEntity, CancellationToken cancellationToken = default);
        Task Delete(string userId, string targetId, CancellationToken cancellationToken = default);

        Task<bool> HasRelation(string userId, string targetId, CancellationToken cancellationToken = default);
        Task<bool> HasStrongRelation(string userId, string targetId, CancellationToken cancellationToken = default);

        Task<long> CountIncomingRelations(string userId, CancellationToken cancellationToken = default);
        Task<long> CountOutgoingRelations(string userId, CancellationToken cancellationToken = default);

        Task<Page<List<UserFollowEntity>>> GetIncomingRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<Page<List<UserFollowEntity>>> GetOutgoingRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default);

        Task DeleteUser(string userId, CancellationToken cancellationToken = default);
    }

    public class UserRelationRepository(IMongoCollection<UserFollowEntity> collection) : IUserRelationRepository
    {
        private readonly IMongoCollection<UserFollowEntity> _collection = collection;

        public async Task Add(UserFollowEntity userFollowEntity, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(userFollowEntity, cancellationToken: cancellationToken);
        }

        public async Task Delete(string userId, string targetId, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteOneAsync(x => x.UserId == userId && x.TargetId == targetId, cancellationToken: cancellationToken);
        }

        public async Task<bool> HasRelation(string userId, string targetId, CancellationToken cancellationToken = default)
        {
            long documentCount = await _collection.CountDocumentsAsync(x => x.UserId == userId && x.TargetId == targetId, cancellationToken: cancellationToken);
            return documentCount == 1;
        }

        public async Task<bool> HasStrongRelation(string userId, string targetId, CancellationToken cancellationToken = default)
        {
            FilterDefinition<UserFollowEntity> oneSideFilter = Builders<UserFollowEntity>.Filter.And(Builders<UserFollowEntity>.Filter.Eq(x => x.UserId, userId), Builders<UserFollowEntity>.Filter.Eq(x => x.TargetId, targetId));
            FilterDefinition<UserFollowEntity> otherSideFilter = Builders<UserFollowEntity>.Filter.And(Builders<UserFollowEntity>.Filter.Eq(x => x.UserId, targetId), Builders<UserFollowEntity>.Filter.Eq(x => x.TargetId, userId));

            FilterDefinition<UserFollowEntity> combinedFilter = Builders<UserFollowEntity>.Filter.Or(oneSideFilter, otherSideFilter);

            long documentCount = await _collection.CountDocumentsAsync(combinedFilter, cancellationToken: cancellationToken);
            return documentCount == 2;
        }

        public async Task<long> CountIncomingRelations(string userId, CancellationToken cancellationToken = default)
        {
            long documentCount = await _collection.CountDocumentsAsync(x => x.TargetId == userId, cancellationToken: cancellationToken);
            return documentCount;
        }

        public async Task<long> CountOutgoingRelations(string userId, CancellationToken cancellationToken = default)
        {
            long documentCount = await _collection.CountDocumentsAsync(x => x.UserId == userId, cancellationToken: cancellationToken);
            return documentCount;
        }

        public async Task<Page<List<UserFollowEntity>>> GetIncomingRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            FindOptions<UserFollowEntity> options = new()
            {
                Limit = page.Size,
                Skip = page.Offset()
            };

            FilterDefinition<UserFollowEntity> filter = Builders<UserFollowEntity>.Filter.Eq(x => x.TargetId, userId);

            long documentCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            IAsyncCursor<UserFollowEntity> cursor = await _collection.FindAsync(filter, options, cancellationToken: cancellationToken);
            List<UserFollowEntity> results = await cursor.ToListAsync(cancellationToken);

            bool hasMoreResults = documentCount > page.Offset() + results.Count;

            return new Page<List<UserFollowEntity>> { Items = results, HasMoreEntries = hasMoreResults };
        }

        public async Task<Page<List<UserFollowEntity>>> GetOutgoingRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            FindOptions<UserFollowEntity> options = new()
            {
                Limit = page.Size,
                Skip = page.Offset()
            };

            FilterDefinition<UserFollowEntity> filter = Builders<UserFollowEntity>.Filter.Eq(x => x.UserId, userId);

            long documentCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            IAsyncCursor<UserFollowEntity> cursor = await _collection.FindAsync(filter, options, cancellationToken: cancellationToken);
            List<UserFollowEntity> results = await cursor.ToListAsync(cancellationToken);

            bool hasMoreResults = documentCount > page.Offset() + results.Count;

            return new Page<List<UserFollowEntity>> { Items = results, HasMoreEntries = hasMoreResults };
        }

        public async Task DeleteUser(string userId, CancellationToken cancellationToken = default)
        {
            await _collection.DeleteManyAsync(x => x.UserId == userId || x.TargetId == userId, cancellationToken: cancellationToken);
        }
    }
}
