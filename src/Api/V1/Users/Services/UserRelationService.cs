using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos.Messages;
using BackBuddy.Api.Service.V1.Users.Entities;
using BackBuddy.Api.Service.V1.Users.Exceptions;
using BackBuddy.Api.Service.V1.Users.Repositories;
using BackBuddy.Api.Service.V1.Utilities;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Users.Services
{
    public interface IUserRelationService
    {
        Task AddRelation(string userId, string targetUserId, CancellationToken cancellationToken = default);
        Task RemoveRelation(string userId, string targetUserId, CancellationToken cancellationToken = default);

        Task<bool> HasRelation(string userId, string targetUserId, CancellationToken cancellationToken = default);
        Task<bool> HasStrongRelation(string userId, string targetUserId, CancellationToken cancellationToken = default);

        Task<long> CountIncomingRelations(string userId, CancellationToken cancellationToken = default);
        Task<long> CountOutgoingRelations(string userId, CancellationToken cancellationToken = default);
        Task<(long IncomingRelations, long OutgoingRelations)> CountRelations(string userId, CancellationToken cancellationToken = default);

        Task<Page<List<string>>> GetIncomingRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<Page<List<string>>> GetOutgoingRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<(Page<List<string>> IncomingRelations, Page<List<string>> OutgoingRelations)> GetRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default);

        Task DeleteUser(string userId, CancellationToken cancellationToken = default);

        Task<UserRelationDto> GetUserRelation(string userId, string targetUserId, CancellationToken cancellationToken = default);
    }

    public class UserRelationService(IUserRelationRepository repository, IPublishEndpoint publishEndpoint) : IUserRelationService
    {
        private readonly IUserRelationRepository _repository = repository;
        private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

        public async Task AddRelation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            if (userId == targetUserId)
                throw new UserCannotFollowThemselvesException();

            bool hasAlreadyRelation = await HasRelation(userId, targetUserId, cancellationToken);
            if (hasAlreadyRelation)
                throw new UserAlreadyFollowingException();

            UserFollowEntity userFollowEntity = new()
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                TargetId = targetUserId,
                CreatedAt = DateTime.UtcNow,
            };

            await _repository.Add(userFollowEntity, cancellationToken);

            // Notify that the user has followed another user
            UserFollowedMessage userFollowedMessage = new()
            {
                UserId = userId,
                TargetUserId = targetUserId,
            };
            await _publishEndpoint.Publish(userFollowedMessage);
        }

        public async Task RemoveRelation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            bool hasRelation = await HasRelation(userId, targetUserId, cancellationToken);
            if (!hasRelation)
                throw new UserNotFollowingException();

            await _repository.Delete(userId, targetUserId, cancellationToken);
        }

        public async Task<long> CountIncomingRelations(string userId, CancellationToken cancellationToken = default)
        {
            return await _repository.CountIncomingRelations(userId, cancellationToken);
        }

        public async Task<long> CountOutgoingRelations(string userId, CancellationToken cancellationToken = default)
        {
            return await _repository.CountOutgoingRelations(userId, cancellationToken);
        }

        public async Task<(long IncomingRelations, long OutgoingRelations)> CountRelations(string userId, CancellationToken cancellationToken = default)
        {
            List<Task<long>> tasks =
            [
                CountIncomingRelations(userId, cancellationToken),
                CountOutgoingRelations(userId, cancellationToken)
            ];
            long[] Relations = await Task.WhenAll(tasks);
            return (Relations[0], Relations[1]);
        }

        public async Task<bool> HasRelation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            return await _repository.HasRelation(userId, targetUserId, cancellationToken);
        }

        public async Task<bool> HasStrongRelation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            return await _repository.HasStrongRelation(userId, targetUserId, cancellationToken);
        }

        public async Task<Page<List<string>>> GetIncomingRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            Page<List<UserFollowEntity>> Relations = await _repository.GetIncomingRelations(userId, page, cancellationToken);
            return new Page<List<string>> { Items = [.. Relations.Items.Select(x => x.UserId)], HasMoreEntries = Relations.HasMoreEntries };
        }

        public async Task<Page<List<string>>> GetOutgoingRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            Page<List<UserFollowEntity>> Relations = await _repository.GetOutgoingRelations(userId, page, cancellationToken);
            return new Page<List<string>> { Items = [.. Relations.Items.Select(x => x.TargetId)], HasMoreEntries = Relations.HasMoreEntries };
        }

        public async Task<(Page<List<string>> IncomingRelations, Page<List<string>> OutgoingRelations)> GetRelations(string userId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            List<Task<Page<List<string>>>> tasks =
            [
                GetIncomingRelations(userId, page, cancellationToken),
                GetOutgoingRelations(userId, page, cancellationToken)
            ];
            Page<List<string>>[] Relations = await Task.WhenAll(tasks);
            return (Relations[0], Relations[1]);
        }

        public async Task DeleteUser(string userId, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteUser(userId, cancellationToken);
        }

        public async Task<UserRelationDto> GetUserRelation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            List<Task<bool>> tasks = [
                HasRelation(userId, targetUserId, cancellationToken),
                HasRelation(targetUserId, userId, cancellationToken)
            ];

            bool[] hasRelations = await Task.WhenAll(tasks);
            return new UserRelationDto
            {
                IsFollowing = hasRelations[0],
                IsFollowedBy = hasRelations[1],
            };
        }
    }
}
