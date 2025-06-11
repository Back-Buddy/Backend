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
    }

    public partial class UserService(FirestoreDb firestore) : IUserService
    {
        private readonly CollectionReference _collection = firestore.Collection("users");

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

        [GeneratedRegex("^[a-zA-Z0-9 ]+$")]
        private static partial Regex InvalidSearchPatternRegex();
    }
}
