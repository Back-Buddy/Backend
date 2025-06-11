using BackBuddy.Api.Service.V1.Database.Firebase;
using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Exceptions;
using Google.Cloud.Firestore;
using System.Text.RegularExpressions;

namespace BackBuddy.Api.Service.V1.Users.Services
{
    public interface IUserService
    {
        Task<IEnumerable<string>> GetUserFCMTokensAsync(string userId);
        Task<List<string>> SearchUser(SearchUserQueryDto query);
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

        public async Task<List<string>> SearchUser(SearchUserQueryDto query)
        {
            if (query.SearchTerm.Trim().Length <= 0)
                throw new InvalidUserSearchPatternException();

            if (!InvalidSearchPatternRegex().IsMatch(query.SearchTerm))
                throw new InvalidUserSearchPatternException();

            QuerySnapshot snapShot = await _collection
                .StartsWith("display_name_upper", query.SearchTerm.Trim().ToUpper())
                .Limit(query.Limit)
                .GetSnapshotAsync();

            IEnumerable<string> ids = snapShot
                                        .Select(x => x.TryGetValue("uid", out string? uid) ? uid : null)
                                        .Where(uid => !string.IsNullOrEmpty(uid))!;
            return [.. ids];
        }

        [GeneratedRegex("^[a-zA-Z0-9]+$")]
        private static partial Regex InvalidSearchPatternRegex();
    }
}
