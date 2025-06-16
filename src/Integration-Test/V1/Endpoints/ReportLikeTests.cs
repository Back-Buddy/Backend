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
        private static UserLib _userLib;

        private readonly static List<Guid> _deviceIds = [];
        private readonly static List<string> _otherUserIds = [];

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _accessToken = Environment.GetEnvironmentVariable("E2E_ACCESS_TOKEN");
            _userId = Environment.GetEnvironmentVariable("E2E_USER_ID");
            Uri baseUri = new(Environment.GetEnvironmentVariable("E2E_BASE_URI") ?? "http://localhost:8080/");

            _deviceLib = new DeviceLib(baseUri.ToString());
            _userLib = new UserLib(baseUri.ToString());
            _reportLib = new ReportLib(baseUri.ToString());

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
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            Guid reportId = Guid.Parse(report["id"].GetValue<string>());

            // Act
            await _reportLib.LikeReport(accessToken2, reportId);

            // Assert
            JsonObject likedReport = await _reportLib.GetReport(accessToken2, reportId, "None");
            Assert.IsNotNull(likedReport);
            Assert.AreEqual(1, likedReport["likeCount"].GetValue<long>());
        }

        [TestMethod]
        public async Task Test_Like_Own_Error()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());

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
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());

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
    }
}
