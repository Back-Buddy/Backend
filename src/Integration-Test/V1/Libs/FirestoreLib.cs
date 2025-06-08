using Google.Api.Gax;
using Google.Cloud.Firestore;

namespace BackBuddy.Integration_Test.V1.Libs
{
    internal class FirestoreLib
    {
        private readonly FirestoreDb _firestoreDb;

        public FirestoreLib(string baseUri, string projectName)
        {
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", baseUri);

            _firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = projectName,
                EmulatorDetection = EmulatorDetection.EmulatorOnly,
            }.Build();
        }

        public async Task CleanUp(string collectionName)
        {
            CollectionReference collection = _firestoreDb.Collection(collectionName);
            QuerySnapshot snapshot = await collection.GetSnapshotAsync();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                await document.Reference.DeleteAsync();
            }
        }

        public async Task<DocumentReference> AddDocumentAsync(string collectionName, object data)
        {
            CollectionReference collection = _firestoreDb.Collection(collectionName);
            DocumentReference document = await collection.AddAsync(data);
            return document;
        }

        public async Task<CollectionReference> GetCollectionAsync(string collectionName)
        {
            CollectionReference collection = _firestoreDb.Collection(collectionName);
            await collection.GetSnapshotAsync();
            return collection;
        }

        public async Task CreateUserObject(string userId, string displayName, IEnumerable<string> fcmTokens)
        {
            Dictionary<string, object> userData = new()
            {
                { "uid", userId },
                { "displayName", displayName }
            };

            DocumentReference userRef = _firestoreDb.Collection("users").Document(userId);
            await userRef.SetAsync(userData);

            foreach (string fcmToken in fcmTokens)
            {
                string tokenId = Guid.NewGuid().ToString();
                Dictionary<string, object> fcmTokenData = new()
                {
                    { "fcm_token", fcmToken },
                    { "createdAt", Timestamp.GetCurrentTimestamp() }
                };
                DocumentReference tokenRef = _firestoreDb.Collection("users").Document(userId).Collection("fcm_tokens").Document(tokenId);
                await tokenRef.SetAsync(fcmTokenData);
            }
        }
    }
}
