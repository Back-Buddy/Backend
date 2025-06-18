using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.V1.Libs;
using System.Text.Json.Nodes;
using static BackBuddy.Integration_Test.V1.DTOs.FirebaseDto;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class ReportLikeTests
    {
        private static DeviceLib _deviceLib;
        private static ReportLib _reportLib;
        private static string _accessToken;
        private static string _userId;
        private static FirebaseLib _firebaseLib;
        private static FirestoreLib _firestoreLib;
        private static NotificationLib _notificationLib;

        private readonly static List<Guid> _deviceIds = [];
        private readonly static List<string> _otherUserIds = [];

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _accessToken = Environment.GetEnvironmentVariable("E2E_ACCESS_TOKEN");
            _userId = Environment.GetEnvironmentVariable("E2E_USER_ID");
            Uri baseUri = new(Environment.GetEnvironmentVariable("E2E_BASE_URI") ?? "http://localhost:8080/");
            Uri notificationUri = new(Environment.GetEnvironmentVariable("E2E_NOTIFICATION_URI") ?? "http://localhost:8083/");

            _deviceLib = new DeviceLib(baseUri.ToString());
            _reportLib = new ReportLib(baseUri.ToString());
            _notificationLib = new NotificationLib(notificationUri.ToString());

            if (_accessToken == null)
            {
                _firebaseLib = new("http://localhost:9099/identitytoolkit.googleapis.com/v1/", "change-me");
                await _firebaseLib.RegisterUserAsync("test@gmail.com", "stringG.1212"); //NOT A REAL SECRET
                FirebaseLoginResponseDto loginResponse = await _firebaseLib.SignInUserAsync("test@gmail.com", "stringG.1212"); //NOT A REAL SECRET
                _userId = loginResponse.LocalId;
                _accessToken = loginResponse.IdToken;
                _firestoreLib = new("http://localhost:8082/", "change-me");
            }
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static async Task ClassCleanup()
        {
            if (_firebaseLib != null)
            {
                await _firebaseLib.DeleteUserAsync(_userId);
                foreach (string otherUserId in _otherUserIds)
                {
                    await _firebaseLib.DeleteUserAsync(otherUserId);
                }
                _otherUserIds.Clear();
            }
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await CleanUpDevices();
            await _firestoreLib.CleanUpUsers();

            foreach (string otherUserId in _otherUserIds)
            {
                await _firebaseLib.DeleteUserAsync(otherUserId);
            }
            _otherUserIds.Clear();
            await _notificationLib.ClearNotifications();
        }

        private static async Task CleanUpDevices()
        {
            foreach (Guid deviceId in _deviceIds)
            {
                await _deviceLib.DeleteDevice(_accessToken, deviceId);
            }
            _deviceIds.Clear();
        }

        [TestMethod]
        public async Task Test_Like_Successful()
        {
            // Arrange
            (string userId2, string accessToken2) = await CreateDefaultUser("test2@gmail.com");

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            // Act
            await _reportLib.LikeReport(accessToken2, reportId);

            // Assert
            JsonObject likedReport = await _reportLib.GetReport(accessToken2, reportId, "None");
            Assert.IsNotNull(likedReport);
            Assert.AreEqual(1, likedReport["likeCount"].GetValue<long>());
            Assert.IsTrue(likedReport["isLikedByRequester"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_Like_Owner_Get_IsLikedByRequester_False()
        {
            // Arrange
            (string userId2, string accessToken2) = await CreateDefaultUser("test2@gmail.com");

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            // Act
            await _reportLib.LikeReport(accessToken2, reportId);

            // Assert
            JsonObject likedReport = await _reportLib.GetReport(_accessToken, reportId, "None");
            Assert.IsNotNull(likedReport);
            Assert.AreEqual(1, likedReport["likeCount"].GetValue<long>());
            Assert.IsFalse(likedReport["isLikedByRequester"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_Like_Not_Liked_Get_IsLikedByRequester_False()
        {
            // Arrange
            (string userId2, string accessToken2) = await CreateDefaultUser("test2@gmail.com");
            (string userId3, string accessToken3) = await CreateDefaultUser("test3@gmail.com");

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);
            await _firestoreLib.CreateUserObject(userId3, "Test User 3", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            // Act
            await _reportLib.LikeReport(accessToken2, reportId);

            // Assert
            JsonObject likedReport = await _reportLib.GetReport(accessToken3, reportId, "None");
            Assert.IsNotNull(likedReport);
            Assert.AreEqual(1, likedReport["likeCount"].GetValue<long>());
            Assert.IsFalse(likedReport["isLikedByRequester"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_Like_Error_Invalid_VisbilityType()
        {
            // Arrange
            (string userId2, string accessToken2) = await CreateDefaultUser("test2@gmail.com");

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            // Act
            RequestFailedException requestFailed = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.LikeReport(accessToken2, reportId));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, requestFailed.ResponseMessage.StatusCode);
        }

        [TestMethod]
        public async Task Test_Like_Own_Error()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            // Act
            RequestFailedException requestFailed = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.LikeReport(_accessToken, reportId));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, requestFailed.ResponseMessage.StatusCode);
        }

        [TestMethod]
        public async Task Test_Like_Double_Like_Error()
        {
            // Arrange
            (string userId2, string accessToken2) = await CreateDefaultUser("test2@gmail.com");

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            // Act
            await _reportLib.LikeReport(accessToken2, reportId);
            RequestFailedException requestFailed = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.LikeReport(accessToken2, reportId));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, requestFailed.ResponseMessage.StatusCode);

            JsonObject likedReport = await _reportLib.GetReport(accessToken2, reportId, "None");
            Assert.IsNotNull(likedReport);
            Assert.AreEqual(1, likedReport["likeCount"].GetValue<long>());
        }

        [TestMethod]
        public async Task Test_GetLikes_Successful()
        {
            // Arrange
            (string userId2, string accessToken2) = await CreateDefaultUser("test2@gmail.com");

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            await _reportLib.LikeReport(accessToken2, reportId);

            // Act
            (JsonArray likes, bool _) = await _reportLib.GetLikes(accessToken2, reportId);

            // Assert
            Assert.AreEqual(1, likes.Count);
            JsonObject like = likes[0].AsObject();
            Assert.AreEqual(userId2, like["userId"].GetValue<string>());
            Assert.AreEqual("Test User 2", like["username"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_GetLikes_Invalid_Visibility_Type()
        {
            // Arrange
            (string userId2, string accessToken2) = await CreateDefaultUser("test2@gmail.com");

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            // Act
            RequestFailedException requestFailed = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.GetLikes(accessToken2, reportId));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, requestFailed.ResponseMessage.StatusCode);
        }


        [TestMethod]
        public async Task Test_GetLikes_Pagination()
        {
            // Arrange
            (string userId2, string accessToken2) = await CreateDefaultUser("test2@gmail.com");
            (string userId3, string accessToken3) = await CreateDefaultUser("test3@gmail.com");
            (string userId4, string accessToken4) = await CreateDefaultUser("test4@gmail.com");
            (string userId5, string accessToken5) = await CreateDefaultUser("test5@gmail.com");
            (string userId6, string accessToken6) = await CreateDefaultUser("test6@gmail.com");

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);
            await _firestoreLib.CreateUserObject(userId3, "Test User 3", []);
            await _firestoreLib.CreateUserObject(userId4, "Test User 4", []);
            await _firestoreLib.CreateUserObject(userId5, "Test User 5", []);
            await _firestoreLib.CreateUserObject(userId6, "Test User 6", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            await _reportLib.LikeReport(accessToken2, reportId);
            await _reportLib.LikeReport(accessToken3, reportId);
            await _reportLib.LikeReport(accessToken4, reportId);
            await _reportLib.LikeReport(accessToken5, reportId);
            await _reportLib.LikeReport(accessToken6, reportId);

            // Act & Assert
            (JsonArray likes, bool hasMoreEntries) = await _reportLib.GetLikes(accessToken2, reportId, page: 1, pageSize: 2);
            Assert.IsTrue(hasMoreEntries);
            Assert.AreEqual(2, likes.Count);

            (JsonArray likes2, bool hasMoreEntries2) = await _reportLib.GetLikes(accessToken2, reportId, page: 2, pageSize: 2);
            Assert.IsTrue(hasMoreEntries2);
            Assert.AreEqual(2, likes2.Count);

            (JsonArray likes3, bool hasMoreEntries3) = await _reportLib.GetLikes(accessToken2, reportId, page: 3, pageSize: 2);
            Assert.IsFalse(hasMoreEntries3);
            Assert.AreEqual(1, likes3.Count);
        }

        [TestMethod]
        public async Task Test_Like_Notification()
        {
            // Arrange
            (string userId2, string accessToken2) = await CreateDefaultUser("test2@gmail.com");

            await _firestoreLib.CreateUserObject(_userId, "Test User", ["token1"]);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            // Act
            await _reportLib.LikeReport(accessToken2, reportId);

            await Task.Delay(2000); // Wait for notification to be processed

            // Assert
            JsonArray notifications = await _notificationLib.GetNotifications();
            Assert.AreEqual(1, notifications.Count);
            JsonObject notification = notifications[0].AsObject();
            JsonArray tokens = notification["tokens"].AsArray();
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual("token1", tokens[0].GetValue<string>());
        }

        private static async Task<(string, string)> CreateDefaultUser(string email)
        {
            FirebaseRegisterResponseDto user = await _firebaseLib.RegisterUserAsync(email, "stringG.1212"); //NOT A REAL SECRET
            string userId = user.LocalId;
            _otherUserIds.Add(userId);
            FirebaseLoginResponseDto loginUser = await _firebaseLib.SignInUserAsync(email, "stringG.1212"); //NOT A REAL SECRET
            string accessToken = loginUser.IdToken;
            return (userId, accessToken);
        }
    }
}
