using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Entities;
using BackBuddy.Api.Service.V1.Users.Exceptions;
using BackBuddy.Api.Service.V1.Users.Repositories;
using BackBuddy.Api.Service.V1.Utilities;

namespace BackBuddy.Api.Service.V1.Users.Services
{
    public interface IUserRelationService
    {
        Task AddRelation(string userId, string targetUserId, CancellationToken cancellationToken = default);
        Task RemoveRelation(string userId, string targetUserId, CancellationToken cancellationToken = default);

        Task<bool> HasReleation(string userId, string targetUserId, CancellationToken cancellationToken = default);
        Task<bool> HasStrongReleation(string userId, string targetUserId, CancellationToken cancellationToken = default);

        Task<long> CountIncomingReleations(string userId, CancellationToken cancellationToken = default);
        Task<long> CountOutgoingReleations(string userId, CancellationToken cancellationToken = default);
        Task<(long IncomingRelations, long OutgoingRelations)> CountReleations(string userId, CancellationToken cancellationToken = default);

        Task<Page<List<string>>> GetIncomingReleations(string userId, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<Page<List<string>>> GetOutgoingReleations(string userId, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<(Page<List<string>> IncomingRelations, Page<List<string>> OutgoingRelations)> GetReleations(string userId, PageRequestDto page, CancellationToken cancellationToken = default);

        Task DeleteUser(string userId, CancellationToken cancellationToken = default);

        Task<UserRelationDto> GetUserRelation(string userId, string targetUserId, CancellationToken cancellationToken = default);
    }

    public class UserReleationService(IUserReleationRepository repository) : IUserRelationService
    {
        private readonly IUserReleationRepository _repository = repository;

        public async Task AddRelation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            if (userId == targetUserId)
                throw new UserCannotFollowThemselfException();

            bool hasAlreadyRelation = await HasReleation(userId, targetUserId, cancellationToken);
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
        }

        public async Task RemoveRelation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            bool hasRelation = await HasReleation(userId, targetUserId, cancellationToken);
            if (!hasRelation)
                throw new UserNotFollowingException();

            await _repository.Delete(userId, targetUserId, cancellationToken);
        }

        public async Task<long> CountIncomingReleations(string userId, CancellationToken cancellationToken = default)
        {
            return await _repository.CountIncomingReleations(userId, cancellationToken);
        }

        public async Task<long> CountOutgoingReleations(string userId, CancellationToken cancellationToken = default)
        {
            return await _repository.CountOutgoingReleations(userId, cancellationToken);
        }

        public async Task<(long IncomingRelations, long OutgoingRelations)> CountReleations(string userId, CancellationToken cancellationToken = default)
        {
            List<Task<long>> tasks =
            [
                CountIncomingReleations(userId, cancellationToken),
                CountOutgoingReleations(userId, cancellationToken)
            ];
            long[] releations = await Task.WhenAll(tasks);
            return (releations[0], releations[1]);
        }

        public async Task<bool> HasReleation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            return await _repository.HasReleation(userId, targetUserId, cancellationToken);
        }

        public async Task<bool> HasStrongReleation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            return await _repository.HasStrongReleation(userId, targetUserId, cancellationToken);
        }

        public async Task<Page<List<string>>> GetIncomingReleations(string userId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            Page<List<UserFollowEntity>> releations = await _repository.GetIncomingReleations(userId, page, cancellationToken);
            return new Page<List<string>> { Items = [.. releations.Items.Select(x => x.UserId)], HasMoreEntries = releations.HasMoreEntries };
        }

        public async Task<Page<List<string>>> GetOutgoingReleations(string userId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            Page<List<UserFollowEntity>> releations = await _repository.GetOutgoingReleations(userId, page, cancellationToken);
            return new Page<List<string>> { Items = [.. releations.Items.Select(x => x.TargetId)], HasMoreEntries = releations.HasMoreEntries };
        }

        public async Task<(Page<List<string>> IncomingRelations, Page<List<string>> OutgoingRelations)> GetReleations(string userId, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            List<Task<Page<List<string>>>> tasks =
            [
                GetIncomingReleations(userId, page, cancellationToken),
                GetOutgoingReleations(userId, page, cancellationToken)
            ];
            Page<List<string>>[] releations = await Task.WhenAll(tasks);
            return (releations[0], releations[1]);
        }

        public async Task DeleteUser(string userId, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteUser(userId, cancellationToken);
        }

        public async Task<UserRelationDto> GetUserRelation(string userId, string targetUserId, CancellationToken cancellationToken = default)
        {
            List<Task<bool>> tasks = [
                HasReleation(userId, targetUserId, cancellationToken),
                HasReleation(targetUserId, userId, cancellationToken)
            ];

            bool[] hasReleations = await Task.WhenAll(tasks);
            return new UserRelationDto
            {
                IsFollowing = hasReleations[0],
                IsFollowedBy = hasReleations[1],
            };
        }
    }
}
