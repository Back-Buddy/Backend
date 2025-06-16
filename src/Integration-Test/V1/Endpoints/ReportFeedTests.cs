using BackBuddy.Integration_Test.V1.Libs;
using System.Text.Json.Nodes;
using static BackBuddy.Integration_Test.V1.DTOs.FirebaseDto;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class ReportFeedTests
    {
        private static DeviceLib _deviceLib;
        private static ReportLib _reportLib;
        private static string _accessToken;
        private static string _userId;
        private static FirebaseLib _firebaseLib;
        private static FirestoreLib _firestoreLib;
        private static UserLib _userLib;

        private readonly static List<(Guid, string)> _deviceIds = [];
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
            foreach ((Guid deviceId, string accessToken) in _deviceIds)
            {
                await _deviceLib.DeleteDevice(accessToken, deviceId);
            }
            _deviceIds.Clear();
        }

        [TestMethod]
        [DataRow("All")]
        [DataRow("Followers")]
        [DataRow("Private")]
        public async Task Test_Feed_Only_Own(string visibilityType)
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, _accessToken));

            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", visibilityType, DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);

            // Act
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken);

            // Assert
            Assert.IsFalse(hasMoreEntries);
            Assert.AreEqual(1, reports.Count);
            Assert.AreEqual(report["id"].GetValue<string>(), reports[0]["id"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_Feed_Other_Following()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(accessToken2, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, accessToken2));

            JsonObject report = await _reportLib.CreateReport(accessToken2, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);

            await _userLib.FollowUser(_accessToken, userId2);

            // Act
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken);

            // Assert
            Assert.IsFalse(hasMoreEntries);
            Assert.AreEqual(1, reports.Count);
            Assert.AreEqual(report["id"].GetValue<string>(), reports[0]["id"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_Feed_Other_Not_Following_VisibilityType_All()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(accessToken2, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, accessToken2));

            await _reportLib.CreateReport(accessToken2, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);

            // Act
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken);

            // Assert
            Assert.IsFalse(hasMoreEntries);
            Assert.AreEqual(0, reports.Count);
        }

        [TestMethod]
        public async Task Test_Feed_Other_Strong_Relation_Visibility_Type_Followers()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(accessToken2, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, accessToken2));

            JsonObject report = await _reportLib.CreateReport(accessToken2, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);

            await _userLib.FollowUser(_accessToken, userId2);
            await _userLib.FollowUser(accessToken2, _userId);

            // Act
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken);

            // Assert
            Assert.IsFalse(hasMoreEntries);
            Assert.AreEqual(1, reports.Count);
            Assert.AreEqual(report["id"].GetValue<string>(), reports[0]["id"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_Feed_Other_No_Strong_Relation_Visibility_Type_Followers()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);

            JsonObject device = await _deviceLib.CreateDevice(accessToken2, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, accessToken2));

            await _reportLib.CreateReport(accessToken2, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);

            await _userLib.FollowUser(_accessToken, userId2);

            // Act
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken);

            // Assert
            Assert.IsFalse(hasMoreEntries);
            Assert.AreEqual(0, reports.Count);
        }

        [TestMethod]
        public async Task Test_Feed_Mixed()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser1 = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser1.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser1 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser1.IdToken;

            FirebaseRegisterResponseDto otherUser2 = await _firebaseLib.RegisterUserAsync("test3@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId3 = otherUser2.LocalId;
            _otherUserIds.Add(userId3);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test3@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken3 = loginUser2.IdToken;

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);
            await _firestoreLib.CreateUserObject(userId3, "Test User 3", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, _accessToken));

            JsonObject deviceUser1 = await _deviceLib.CreateDevice(accessToken2, "TestDevice");
            Guid deviceIdUser1 = Guid.Parse(deviceUser1["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceIdUser1, accessToken2));

            JsonObject deviceUser2 = await _deviceLib.CreateDevice(accessToken3, "TestDevice");
            Guid deviceIdUser2 = Guid.Parse(deviceUser2["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceIdUser2, accessToken3));

            JsonObject report1 = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report 1", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            JsonObject report2 = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report 2", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            JsonObject report3 = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report 3", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            JsonObject report4 = await _reportLib.CreateReport(accessToken2, deviceIdUser1, "Test Report 4", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken2, deviceIdUser1, "Test Report 5", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            JsonObject report6 = await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 6", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 7", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);

            await _userLib.FollowUser(_accessToken, userId2);
            await _userLib.FollowUser(accessToken2, _userId);

            await _userLib.FollowUser(_accessToken, userId3);

            // Act
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken);

            // Assert
            Assert.IsFalse(hasMoreEntries);
            Assert.AreEqual(5, reports.Count);
            Assert.AreEqual(reports[0]["id"].GetValue<string>(), report6["id"].GetValue<string>());
            Assert.AreEqual(reports[1]["id"].GetValue<string>(), report4["id"].GetValue<string>());
            Assert.AreEqual(reports[2]["id"].GetValue<string>(), report3["id"].GetValue<string>());
            Assert.AreEqual(reports[3]["id"].GetValue<string>(), report2["id"].GetValue<string>());
            Assert.AreEqual(reports[4]["id"].GetValue<string>(), report1["id"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_Feed_Mixed_Ascending()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser1 = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser1.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser1 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser1.IdToken;

            FirebaseRegisterResponseDto otherUser2 = await _firebaseLib.RegisterUserAsync("test3@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId3 = otherUser2.LocalId;
            _otherUserIds.Add(userId3);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test3@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken3 = loginUser2.IdToken;

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);
            await _firestoreLib.CreateUserObject(userId3, "Test User 3", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, _accessToken));

            JsonObject deviceUser1 = await _deviceLib.CreateDevice(accessToken2, "TestDevice");
            Guid deviceIdUser1 = Guid.Parse(deviceUser1["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceIdUser1, accessToken2));

            JsonObject deviceUser2 = await _deviceLib.CreateDevice(accessToken3, "TestDevice");
            Guid deviceIdUser2 = Guid.Parse(deviceUser2["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceIdUser2, accessToken3));

            JsonObject report1 = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report 1", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            JsonObject report2 = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report 2", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            JsonObject report3 = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report 3", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            JsonObject report4 = await _reportLib.CreateReport(accessToken2, deviceIdUser1, "Test Report 4", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken2, deviceIdUser1, "Test Report 5", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            JsonObject report6 = await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 6", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 7", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);

            await _userLib.FollowUser(_accessToken, userId2);
            await _userLib.FollowUser(accessToken2, _userId);

            await _userLib.FollowUser(_accessToken, userId3);

            // Act
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken, descending: false);

            // Assert
            Assert.IsFalse(hasMoreEntries);
            Assert.AreEqual(5, reports.Count);
            Assert.AreEqual(reports[4]["id"].GetValue<string>(), report6["id"].GetValue<string>());
            Assert.AreEqual(reports[3]["id"].GetValue<string>(), report4["id"].GetValue<string>());
            Assert.AreEqual(reports[2]["id"].GetValue<string>(), report3["id"].GetValue<string>());
            Assert.AreEqual(reports[1]["id"].GetValue<string>(), report2["id"].GetValue<string>());
            Assert.AreEqual(reports[0]["id"].GetValue<string>(), report1["id"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_Feed_Expand_Type_Device_Logs()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, _accessToken));

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);

            // Act
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken, expandType: "DeviceLogs");

            // Assert
            Assert.IsFalse(hasMoreEntries);
            Assert.AreEqual(1, reports.Count);
            Assert.AreEqual(0, reports[0]["usedLogs"].AsArray().Count);
        }

        [TestMethod]
        public async Task Test_Feed_Expand_Type_None()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, _accessToken));

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);

            // Act
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken, expandType: "None");

            // Assert
            Assert.IsFalse(hasMoreEntries);
            Assert.AreEqual(1, reports.Count);
            Assert.IsNull(reports[0]["usedLogs"]);
        }

        [TestMethod]
        public async Task Test_Feed_Mixed_Pagination()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser1 = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser1.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser1 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser1.IdToken;

            FirebaseRegisterResponseDto otherUser2 = await _firebaseLib.RegisterUserAsync("test3@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId3 = otherUser2.LocalId;
            _otherUserIds.Add(userId3);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test3@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken3 = loginUser2.IdToken;

            await _firestoreLib.CreateUserObject(_userId, "Test User", []);
            await _firestoreLib.CreateUserObject(userId2, "Test User 2", []);
            await _firestoreLib.CreateUserObject(userId3, "Test User 3", []);

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceId, _accessToken));

            JsonObject deviceUser1 = await _deviceLib.CreateDevice(accessToken2, "TestDevice");
            Guid deviceIdUser1 = Guid.Parse(deviceUser1["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceIdUser1, accessToken2));

            JsonObject deviceUser2 = await _deviceLib.CreateDevice(accessToken3, "TestDevice");
            Guid deviceIdUser2 = Guid.Parse(deviceUser2["deviceId"].GetValue<string>());
            _deviceIds.Add((deviceIdUser2, accessToken3));

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report 1", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report 2", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report 3", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken2, deviceIdUser1, "Test Report 4", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken2, deviceIdUser1, "Test Report 4", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken2, deviceIdUser1, "Test Report 4", "Followers", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken2, deviceIdUser1, "Test Report 5", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 6", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 7", "Private", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 6", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 6", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 6", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
            await _reportLib.CreateReport(accessToken3, deviceIdUser2, "Test Report 6", "All", DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);


            await _userLib.FollowUser(_accessToken, userId2);
            await _userLib.FollowUser(accessToken2, _userId);

            await _userLib.FollowUser(_accessToken, userId3);

            // Act & Assert
            (JsonArray reports, bool hasMoreEntries) = await _reportLib.GetFeed(_accessToken, descending: false, page: 1, pageSize: 5);
            Assert.IsTrue(hasMoreEntries);
            Assert.AreEqual(5, reports.Count);

            (JsonArray reports2, bool hasMoreEntries2) = await _reportLib.GetFeed(_accessToken, descending: false, page: 2, pageSize: 5);
            Assert.IsTrue(hasMoreEntries2);
            Assert.AreEqual(5, reports2.Count);

            (JsonArray reports3, bool hasMoreEntries3) = await _reportLib.GetFeed(_accessToken, descending: false, page: 3, pageSize: 5);
            Assert.IsFalse(hasMoreEntries3);
            Assert.AreEqual(1, reports3.Count);
        }
    }
}
