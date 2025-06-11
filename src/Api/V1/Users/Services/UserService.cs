using BackBuddy.Api.Service.V1.Database.Firebase;
using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos.Http;
using BackBuddy.Api.Service.V1.Users.Exceptions;
using BackBuddy.Api.Service.V1.Users.Mapper;
using Google.Cloud.Firestore;
using System.Text.RegularExpressions;

namespace BackBuddy.Api.Service.V1.Users.Services
{
    public interface IUserService
    {
        Task<IEnumerable<string>> GetUserFCMTokensAsync(string userId);
        Task<IEnumerable<UserDto>> SearchUser(SearchUserQueryDto query);
        Task<bool> IsUserIdValid(string userId);
        Task<UserDto> GetUserByIdAsync(string userId);
        Task<List<UserDto>> GetUsers(List<string> users);
    }

    public partial class UserService(FirestoreDb firestore, ILogger<UserService> logger) : IUserService
    {
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

        public async Task<IEnumerable<UserDto>> SearchUser(SearchUserQueryDto query)
        {
            if (query.SearchTerm.Trim().Length <= 0)
                throw new InvalidUserSearchPatternException();

            if (!InvalidSearchPatternRegex().IsMatch(query.SearchTerm))
                throw new InvalidUserSearchPatternException();

            QuerySnapshot querySnapshot = await _collection
                .StartsWith("display_name_upper", query.SearchTerm.Trim().ToUpper())
                .Limit(query.Limit)
                .GetSnapshotAsync();

            return querySnapshot.ToDtos();
        }

        public async Task<bool> IsUserIdValid(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;
            DocumentSnapshot documentSnapshot = await _collection.Document(userId).GetSnapshotAsync();
            return documentSnapshot.Exists;
        }

        public async Task<UserDto> GetUserByIdAsync(string userId)
        {
            DocumentSnapshot documentSnapshot = await _collection.Document(userId).GetSnapshotAsync();
            if (!documentSnapshot.Exists)
                throw new UserNotFoundException();
            return documentSnapshot.ToDto() ?? throw new UserNotFoundException();
        }

        public async Task<List<UserDto>> GetUsers(List<string> users)
        {
            IEnumerable<Task<UserDto?>> tasks = users.Select(async userId =>
            {
                try
                {
                    return await GetUserByIdAsync(userId);
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

        [GeneratedRegex("^[a-zA-Z0-9 ]+$")]
        private static partial Regex InvalidSearchPatternRegex();

    }
}
