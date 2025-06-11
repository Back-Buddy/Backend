using BackBuddy.Api.Service.V1.Database.Firebase;
using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos.Http;
using BackBuddy.Api.Service.V1.Users.Enums;
using BackBuddy.Api.Service.V1.Users.Exceptions;
using BackBuddy.Api.Service.V1.Users.Mapper;
using Google.Cloud.Firestore;
using System.Text.RegularExpressions;

namespace BackBuddy.Api.Service.V1.Users.Services
{
    public interface IUserService
    {
        Task<IEnumerable<string>> GetUserFCMTokensAsync(string userId);
        Task<IEnumerable<UserDto>> SearchUser(SearchUserQueryDto query, UserExpandType userExpandType = UserExpandType.None);
        Task<bool> IsUserIdValid(string userId);
        Task<UserDto> GetUserByIdAsync(string userId, UserExpandType userExpandType = UserExpandType.None);
        Task<List<UserDto>> GetUsers(List<string> users, UserExpandType expandType = UserExpandType.None);
        Task DeleteUser(string userId);
    }

    public partial class UserService(IUserRelationService userRelationService, FirestoreDb firestore, ILogger<UserService> logger) : IUserService
    {
        private readonly IUserRelationService _userRelationService = userRelationService;
        private readonly CollectionReference _collection = firestore.Collection("users");
        private readonly ILogger<UserService> _logger = logger;

        public async Task<IEnumerable<string>> GetUserFCMTokensAsync(string userId)
        {
            CollectionReference fcmTokenCollection = _collection.Document(userId).Collection("fcm_tokens");
            QuerySnapshot snapshots = await fcmTokenCollection.GetSnapshotAsync();

            IEnumerable<string?> tokens = snapshots.Select(document => document.TryGetValue("fcm_token", out string token) ? token : null)
                                            .Where(token => !string.IsNullOrEmpty(token));
            return tokens!;
        }

        public async Task<IEnumerable<UserDto>> SearchUser(SearchUserQueryDto query, UserExpandType userExpandType = UserExpandType.None)
        {
            if (query.SearchTerm.Trim().Length <= 0)
                throw new InvalidUserSearchPatternException();

            if (!InvalidSearchPatternRegex().IsMatch(query.SearchTerm))
                throw new InvalidUserSearchPatternException();

            QuerySnapshot querySnapshot = await _collection
                .StartsWith("display_name_upper", query.SearchTerm.Trim().ToUpper())
                .Limit(query.Limit)
                .GetSnapshotAsync();

            IEnumerable<UserDto> users = querySnapshot.ToDtos();
            if (!users.Any())
                return users;

            if (userExpandType == UserExpandType.Relations)
            {
                IEnumerable<Task<UserDto>> tasks = users.Select(async user =>
                {
                    (long incomingRelations, long outgoingRelations) = await _userRelationService.CountRelations(user.UserId);
                    user.Followers = incomingRelations;
                    user.Following = outgoingRelations;
                    return user;
                });
                UserDto[] userDtos = await Task.WhenAll(tasks);
                users = userDtos;
            }
            return users;
        }

        public async Task<bool> IsUserIdValid(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;
            DocumentSnapshot documentSnapshot = await _collection.Document(userId).GetSnapshotAsync();
            return documentSnapshot.Exists;
        }

        public async Task<UserDto> GetUserByIdAsync(string userId, UserExpandType userExpandType = UserExpandType.None)
        {
            DocumentSnapshot documentSnapshot = await _collection.Document(userId).GetSnapshotAsync();
            if (!documentSnapshot.Exists)
                throw new UserNotFoundException();
            UserDto userDto = documentSnapshot.ToDto() ?? throw new UserNotFoundException();

            if (userExpandType == UserExpandType.Relations)
            {
                (long incomingRelations, long outgoingRelations) = await _userRelationService.CountRelations(userId);
                userDto.Followers = incomingRelations;
                userDto.Following = outgoingRelations;
            }
            return userDto;
        }

        public async Task<List<UserDto>> GetUsers(List<string> users, UserExpandType userExpandType = UserExpandType.None)
        {
            IEnumerable<Task<UserDto?>> tasks = users.Select(async userId =>
            {
                try
                {
                    return await GetUserByIdAsync(userId, userExpandType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while getting user {UserId}", userId);
                    return null;
                }
            });
            UserDto?[] result = await Task.WhenAll(tasks);
            return [.. result.Where(x => x != null)!];
        }

        public async Task DeleteUser(string userId)
        {
            await _userRelationService.DeleteUser(userId);
        }

        [GeneratedRegex("^[a-zA-Z0-9 ]+$")]
        private static partial Regex InvalidSearchPatternRegex();

    }
}
