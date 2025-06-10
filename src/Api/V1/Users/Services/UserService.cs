using Google.Cloud.Firestore;

namespace BackBuddy.Api.Service.V1.Users.Services
{
    public interface IUserService
    {
        Task<IEnumerable<string>> GetUserFCMTokensAsync(string userId);
    }

    public class UserService(FirestoreDb firestore) : IUserService
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
    }
}
