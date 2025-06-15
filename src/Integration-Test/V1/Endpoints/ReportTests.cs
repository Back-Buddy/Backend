using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Globalization;
using System.Text.Json.Nodes;
using static BackBuddy.Integration_Test.V1.DTOs.FirebaseDto;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class ReportTests
    {
        private static DeviceLib _deviceLib;
        private static ReportLib _reportLib;
        private static string _accessToken;
        private static string _userId;
        private static string _webSocketUri;
        private static FirebaseLib _firebaseLib;
        private static FirestoreLib _firestoreLib;
        private static UserLib _userLib;

        private readonly static List<Guid> _deviceIds = [];
        private readonly static List<string> _otherUserIds = [];

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _webSocketUri = Environment.GetEnvironmentVariable("E2E_WEBSOCKET_URI") ?? "ws://localhost:8080/";
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
                FirebaseDto.FirebaseLoginResponseDto loginResponse = await _firebaseLib.SignInUserAsync("test@gmail.com", "stringG.1212"); //NOT A REAL SECRET
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
        public async Task Test_Create_Report_Success()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            string name = "Test Report";
            string visibilityType = "All";
            TimeSpan sitDuration = TimeSpan.FromSeconds(5);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;

            // Act
            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, name, visibilityType, startTime, endTime);

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(deviceId, Guid.Parse(report["deviceId"].GetValue<string>()));
            Assert.AreEqual(name, report["name"].GetValue<string>());
            Assert.AreEqual(visibilityType, report["visibilityType"].GetValue<string>());
            Assert.AreEqual(startTime, report["startTime"].GetValue<DateTime>());
            Assert.AreEqual(endTime, report["endTime"].GetValue<DateTime>());

            Assert.AreEqual(1, report["usedLogsIds"].AsArray().Count);

            JsonObject metaData = report["metadata"].AsObject();
            Assert.IsNotNull(metaData);
            Assert.IsTrue(TimeSpan.Parse(metaData["sitTime"].GetValue<string>(), new CultureInfo("en-US")) > TimeSpan.Zero, "Sittime must be greater than 0");
            Assert.IsTrue(Math.Abs((TimeSpan.Parse(metaData["sitTime"].GetValue<string>(), new CultureInfo("en-US")) - sitDuration).TotalSeconds) < 0.5, "Sittime and Sitduration must be equal (Threshold: 0.5)");
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("a")]
        [DataRow("ab")]
        [DataRow("thisstringiswaytoolongtobevalidbecauseitexceeds32characters_andnowweareaddingmoredatatofilluptoexactlyonehundredtwentyeight1")]
        [DataRow("name_with_underscore")]
        [DataRow("name.with.dot")]
        [DataRow("name@domain")]
        [DataRow("namé")]
        [DataRow("na🚀me")]
        [DataRow("na\tme")]
        [DataRow("na\nme")]
        [DataRow("name!")]
        [DataRow("na#me")]

        public async Task Test_Create_Report_Invalid_Name(string name)
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            // Act
            RequestFailedException exception = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.CreateReport(_accessToken, deviceId, name, "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, exception.ResponseMessage.StatusCode);
        }

        [TestMethod]
        [DataRow("abc")]
        [DataRow("Test123")]
        [DataRow("Valid-Name")]
        [DataRow("Name With Spaces")]
        [DataRow("A1-B2 C3")]
        [DataRow("SimpleName")]
        [DataRow("Name-Name-Name")]
        [DataRow("Name With-Mixed Characters")]
        [DataRow("aB3")]
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZ123456")]
        public async Task Test_Create_Report_Valid_Name(string name)
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            // Act
            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, name, "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            // Assert
            Assert.IsNotNull(report);
            Assert.AreEqual(name, report["name"].GetValue<string>());
        }

        [TestMethod]
        [DataRow("All")]
        [DataRow("Followers")]
        [DataRow("Private")]
        public async Task Test_Create_Report_Valid_Visibility(string validVisibility)
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            // Act
            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", validVisibility, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            // Assert
            Assert.IsNotNull(report);
            Assert.AreEqual(validVisibility, report["visibilityType"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_Create_Report_Invalid_Visibility()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);
            string invalidVisibility = "test";

            // Act
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", invalidVisibility, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, requestFailedException.ResponseMessage.StatusCode);
        }

        [TestMethod]
        public async Task Test_Create_Report_No_Logs()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;

            // Act
            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", startTime, endTime);

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(deviceId, Guid.Parse(report["deviceId"].GetValue<string>()));

            Assert.IsEmpty(report["usedLogsIds"].AsArray());
        }

        [TestMethod]
        public async Task Test_Create_Report_More_Logs()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);

            TimeSpan sitDuration = TimeSpan.FromMilliseconds(500);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 10, 0, sitDuration);

            DateTime endTime = DateTime.UtcNow;

            // Act
            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", startTime, endTime);

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(deviceId, Guid.Parse(report["deviceId"].GetValue<string>()));

            Assert.AreEqual(10, report["usedLogsIds"].AsArray().Count);
        }

        [TestMethod]
        public async Task Test_GetReport_Success()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(1);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", startTime, endTime);

            // Act
            JsonObject getReport = await _reportLib.GetReport(_accessToken, Guid.Parse(createdReport["id"].GetValue<string>()));

            // Assert
            Assert.IsNotNull(getReport);
            Assert.IsTrue(getReport.ContainsKey("id"));
            Assert.AreEqual(createdReport["id"].GetValue<string>(), getReport["id"].GetValue<string>());
            Assert.AreEqual(deviceId, Guid.Parse(getReport["deviceId"].GetValue<string>()));
            Assert.AreEqual(startTime.ToString("f"), getReport["startTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(endTime.ToString("f"), getReport["endTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(1, getReport["usedLogsIds"].AsArray().Count);
            Assert.IsNull(getReport["usedLogs"]);
        }

        [TestMethod]
        public async Task Test_GetReport_Other_User_Success()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(1);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", startTime, endTime);

            // Act
            JsonObject getReport = await _reportLib.GetReport(accessToken2, Guid.Parse(createdReport["id"].GetValue<string>()));

            // Assert
            Assert.IsNotNull(getReport);
            Assert.IsTrue(getReport.ContainsKey("id"));
            Assert.AreEqual(createdReport["id"].GetValue<string>(), getReport["id"].GetValue<string>());
            Assert.IsNull(getReport["deviceId"]);
            Assert.AreEqual(startTime.ToString("f"), getReport["startTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(endTime.ToString("f"), getReport["endTime"].GetValue<DateTime>().ToString("f"));
            Assert.IsNull(getReport["usedLogsIds"]);
            Assert.IsNull(getReport["usedLogs"]);
        }

        [TestMethod]
        public async Task Test_GetReport_Other_User_Expand_Success()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(1);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", startTime, endTime);

            // Act
            JsonObject getReport = await _reportLib.GetReport(accessToken2, Guid.Parse(createdReport["id"].GetValue<string>()), expandType: "DeviceLogs");

            // Assert
            Assert.IsNotNull(getReport);
            Assert.IsTrue(getReport.ContainsKey("id"));
            Assert.AreEqual(createdReport["id"].GetValue<string>(), getReport["id"].GetValue<string>());
            Assert.IsNull(getReport["deviceId"]);
            Assert.AreEqual(startTime.ToString("f"), getReport["startTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(endTime.ToString("f"), getReport["endTime"].GetValue<DateTime>().ToString("f"));
            Assert.IsNull(getReport["usedLogsIds"]);
            Assert.AreEqual(1, getReport["usedLogs"].AsArray().Count);
        }

        [TestMethod]
        public async Task Test_GetReport_Other_User_VisibilityType_Followers_Failed()
        {
            // Arrange
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(1);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", startTime, endTime);

            // Act
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.GetReport(accessToken2, Guid.Parse(createdReport["id"].GetValue<string>())));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, requestFailedException.ResponseMessage.StatusCode);
        }

        [TestMethod]
        public async Task Test_GetReport_Other_User_VisibilityType_Followers_Success()
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
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(1);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", startTime, endTime);

            await _userLib.FollowUser(_accessToken, userId2);
            await _userLib.FollowUser(accessToken2, _userId);

            // Act
            JsonObject getReport = await _reportLib.GetReport(accessToken2, Guid.Parse(createdReport["id"].GetValue<string>()), expandType: "DeviceLogs");

            // Assert
            Assert.IsNotNull(getReport);
            Assert.IsTrue(getReport.ContainsKey("id"));
            Assert.AreEqual(createdReport["id"].GetValue<string>(), getReport["id"].GetValue<string>());
            Assert.IsNull(getReport["deviceId"]);
            Assert.AreEqual(startTime.ToString("f"), getReport["startTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(endTime.ToString("f"), getReport["endTime"].GetValue<DateTime>().ToString("f"));
            Assert.IsNull(getReport["usedLogsIds"]);
            Assert.AreEqual(1, getReport["usedLogs"].AsArray().Count);
        }

        [TestMethod]
        public async Task Test_GetReport_Other_User_VisibilityType_Private_Failed()
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
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(1);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", startTime, endTime);

            // Act & Assert
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.GetReport(accessToken2, Guid.Parse(createdReport["id"].GetValue<string>())));
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, requestFailedException.ResponseMessage.StatusCode);

            await _userLib.FollowUser(_accessToken, userId2);
            await _userLib.FollowUser(accessToken2, _userId);

            requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.GetReport(accessToken2, Guid.Parse(createdReport["id"].GetValue<string>())));
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, requestFailedException.ResponseMessage.StatusCode);
        }

        [TestMethod]
        public async Task Test_GetReport_VisibilityType_Private_Success()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(1);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", startTime, endTime);

            // Act
            JsonObject getReport = await _reportLib.GetReport(_accessToken, Guid.Parse(createdReport["id"].GetValue<string>()));

            // Assert
            Assert.IsNotNull(getReport);
            Assert.IsTrue(getReport.ContainsKey("id"));
            Assert.AreEqual(createdReport["id"].GetValue<string>(), getReport["id"].GetValue<string>());
            Assert.AreEqual(deviceId, Guid.Parse(getReport["deviceId"].GetValue<string>()));
            Assert.AreEqual(startTime.ToString("f"), getReport["startTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(endTime.ToString("f"), getReport["endTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(1, getReport["usedLogsIds"].AsArray().Count);
            Assert.IsNull(getReport["usedLogs"]);
        }

        [TestMethod]
        public async Task Test_GetReport_Success_Expand_Type()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(1);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);
            DateTime endTime = DateTime.UtcNow;

            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", startTime, endTime);

            // Act
            JsonObject getReport = await _reportLib.GetReport(_accessToken, Guid.Parse(createdReport["id"].GetValue<string>()), expandType: "DeviceLogs");

            // Assert
            Assert.IsNotNull(getReport);
            Assert.IsTrue(getReport.ContainsKey("id"));
            Assert.AreEqual(createdReport["id"].GetValue<string>(), getReport["id"].GetValue<string>());
            Assert.AreEqual(deviceId, Guid.Parse(getReport["deviceId"].GetValue<string>()));
            Assert.AreEqual(startTime.ToString("f"), getReport["startTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(endTime.ToString("f"), getReport["endTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(1, getReport["usedLogsIds"].AsArray().Count);
            Assert.AreEqual(1, getReport["usedLogs"].AsArray().Count);
        }

        [TestMethod]
        public async Task Test_DeleteReport_Success()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(1);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", startTime, endTime);
            Guid reportId = Guid.Parse(createdReport["id"].GetValue<string>());

            // Act
            await _reportLib.DeleteReport(_accessToken, reportId);

            // Assert
            RequestFailedException exception = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.GetReport(_accessToken, reportId));
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, exception.ResponseMessage.StatusCode);
        }

        [TestMethod]
        public async Task Test_GetReports_Pagination()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await _reportLib.CreateSampleReports(_webSocketUri, secret, _accessToken, deviceId, "Test Report", "All", 8, TimeSpan.FromSeconds(2));

            // Act & Assert
            (JsonArray result, bool hasMoreResults) = await _reportLib.GetReports(_accessToken, [deviceId], pageSize: 3, page: 1);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(hasMoreResults, "There should be more results available");

            (JsonArray nextPageResult, bool hasMoreNextPageResults) = await _reportLib.GetReports(_accessToken, [deviceId], pageSize: 3, page: 2);
            Assert.AreEqual(3, nextPageResult.Count);
            Assert.IsTrue(hasMoreNextPageResults, "There should be more results available");

            (JsonArray lastPageResult, bool hasMoreLastPageResults) = await _reportLib.GetReports(_accessToken, [deviceId], pageSize: 3, page: 3);
            Assert.AreEqual(2, lastPageResult.Count);
            Assert.IsFalse(hasMoreLastPageResults, "There should not be more results available on the last page");
        }

        [TestMethod]
        public async Task Test_GetReports_Sorting()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await _reportLib.CreateSampleReports(_webSocketUri, secret, _accessToken, deviceId, "Test Report", "All", 5, TimeSpan.FromSeconds(1));

            // Act
            (JsonArray resultDescending, _) = await _reportLib.GetReports(_accessToken, [deviceId], descending: true, pageSize: 5, page: 1);
            (JsonArray resultAscending, _) = await _reportLib.GetReports(_accessToken, [deviceId], descending: false, pageSize: 5, page: 1);

            // Assert
            Assert.AreEqual(5, resultDescending.Count);
            Assert.AreEqual(5, resultAscending.Count);

            Assert.IsTrue(resultDescending[0]["createdAt"].GetValue<DateTime>() >= resultDescending[4]["createdAt"].GetValue<DateTime>(), "Descending order failed");
            Assert.IsTrue(resultAscending[0]["createdAt"].GetValue<DateTime>() <= resultAscending[4]["createdAt"].GetValue<DateTime>(), "Ascending order failed");
            Assert.AreEqual(resultDescending[0]["id"].GetValue<string>(), resultAscending[4]["id"].GetValue<string>(), "First report in descending order should match last report in ascending order");
        }

        [TestMethod]
        public async Task Test_GetReports_StartDate()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await _reportLib.CreateSampleReports(_webSocketUri, secret, _accessToken, deviceId, "Test Report", "All", 5, TimeSpan.FromSeconds(1));
            (JsonArray result, _) = await _reportLib.GetReports(_accessToken, [deviceId], descending: false, pageSize: 5, page: 1);

            // Act
            DateTime startTime = result[0].AsObject()["startTime"].GetValue<DateTime>();
            (JsonArray filteredResult, _) = await _reportLib.GetReports(_accessToken, [deviceId], descending: false, startTime: startTime.AddSeconds(1), pageSize: 5, page: 1);

            // Assert
            Assert.IsNotEmpty(filteredResult);
            Assert.AreNotEqual(filteredResult[0]["id"].GetValue<string>(), result[0]["id"].GetValue<string>());
            Assert.AreNotEqual(result.Count, filteredResult.Count, "Filtered result should not contain the first report");
        }

        [TestMethod]
        public async Task Test_GetReports_Expand_Type_DeviceLogs()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await _reportLib.CreateSampleReports(_webSocketUri, secret, _accessToken, deviceId, "Test Report", "All", 1, TimeSpan.FromSeconds(1));

            // Act
            (JsonArray result, _) = await _reportLib.GetReports(_accessToken, [deviceId], descending: false, pageSize: 5, page: 1, expandType: "DeviceLogs");

            // Assert
            JsonObject report = result[0].AsObject();

            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(report["id"].GetValue<string>(), report["id"].GetValue<string>());
            Assert.IsNotNull(report["deviceId"]);
            Assert.AreEqual(1, report["usedLogsIds"].AsArray().Count);
            Assert.AreEqual(1, report["usedLogs"].AsArray().Count);
        }

        [TestMethod]
        public async Task Test_GetReports_Expand_Type_None()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await _reportLib.CreateSampleReports(_webSocketUri, secret, _accessToken, deviceId, "Test Report", "All", 1, TimeSpan.FromSeconds(1));

            // Act
            (JsonArray result, _) = await _reportLib.GetReports(_accessToken, [deviceId], descending: false, pageSize: 5, page: 1);

            // Assert
            JsonObject report = result[0].AsObject();

            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(report["id"].GetValue<string>(), report["id"].GetValue<string>());
            Assert.IsNotNull(report["deviceId"]);
            Assert.AreEqual(1, report["usedLogsIds"].AsArray().Count);
            Assert.IsNull(report["usedLogs"]);
        }

        [TestMethod]
        public async Task Test_GetReports_Other_User_VisibilityType_All()
        {
            // Arrange 
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            // Act
            (JsonArray filteredResult, _) = await _reportLib.GetReports(accessToken2, [], descending: false, pageSize: 5, page: 1, userId: _userId);

            // Assert
            Assert.AreEqual(3, filteredResult.Count);
        }

        [TestMethod]
        public async Task Test_GetReports_Other_User_Expand_Type_None()
        {
            // Arrange 
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            // Act
            (JsonArray filteredResult, _) = await _reportLib.GetReports(accessToken2, [], descending: false, pageSize: 5, page: 1, userId: _userId);

            // Assert
            Assert.AreEqual(3, filteredResult.Count);

            JsonObject report = filteredResult[0].AsObject();

            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(report["id"].GetValue<string>(), report["id"].GetValue<string>());
            Assert.IsNull(report["deviceId"]);
            Assert.IsNull(report["usedLogsIds"]);
            Assert.IsNull(report["usedLogs"]);
        }

        [TestMethod]
        public async Task Test_GetReports_Other_User_Expand_Type_DeviceLogs()
        {
            // Arrange 
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            // Act
            (JsonArray filteredResult, _) = await _reportLib.GetReports(accessToken2, [], descending: false, pageSize: 5, page: 1, userId: _userId, expandType: "DeviceLogs");

            // Assert
            Assert.AreEqual(3, filteredResult.Count);

            JsonObject report = filteredResult[0].AsObject();

            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(report["id"].GetValue<string>(), report["id"].GetValue<string>());
            Assert.IsNull(report["deviceId"]);
            Assert.IsNull(report["usedLogsIds"]);
            Assert.AreEqual(0, report["usedLogs"].AsArray().Count);
        }

        [TestMethod]
        public async Task Test_GetReports_Other_User_VisibilityType_Private()
        {
            // Arrange 
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            // Act
            (JsonArray filteredResult, _) = await _reportLib.GetReports(accessToken2, [], descending: false, pageSize: 5, page: 1, userId: _userId);

            // Assert
            Assert.AreEqual(0, filteredResult.Count);
        }

        [TestMethod]
        public async Task Test_GetReports_Other_User_VisibilityType_Followers()
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
            _deviceIds.Add(deviceId);

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            await _userLib.FollowUser(_accessToken, userId2);
            await _userLib.FollowUser(accessToken2, _userId);

            // Act
            (JsonArray filteredResult, _) = await _reportLib.GetReports(accessToken2, [], descending: false, pageSize: 5, page: 1, userId: _userId);

            // Assert
            Assert.AreEqual(3, filteredResult.Count);
        }

        [TestMethod]
        public async Task Test_GetReports_Other_User_VisibilityType_Followers_No_Strong_Relation()
        {
            // Arrange 
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            // Act
            (JsonArray filteredResult, _) = await _reportLib.GetReports(accessToken2, [], descending: false, pageSize: 5, page: 1, userId: _userId);

            // Assert
            Assert.AreEqual(0, filteredResult.Count);
        }

        [TestMethod]
        public async Task Test_GetReports_Other_User_VisibilityType_Mixed_No_Strong_Relation()
        {
            // Arrange 
            FirebaseRegisterResponseDto otherUser = await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string userId2 = otherUser.LocalId;
            _otherUserIds.Add(userId2);
            FirebaseLoginResponseDto loginUser2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
            string accessToken2 = loginUser2.IdToken;

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            // Act
            (JsonArray filteredResult, _) = await _reportLib.GetReports(accessToken2, [], descending: false, pageSize: 5, page: 1, userId: _userId);

            // Assert
            Assert.AreEqual(1, filteredResult.Count);
        }

        [TestMethod]
        public async Task Test_GetReports_Other_User_VisibilityType_Mixed_Strong_Relation()
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
            _deviceIds.Add(deviceId);

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            await _userLib.FollowUser(_accessToken, userId2);
            await _userLib.FollowUser(accessToken2, _userId);

            // Act
            (JsonArray filteredResult, _) = await _reportLib.GetReports(accessToken2, [], descending: false, pageSize: 5, page: 1, userId: _userId);

            // Assert
            Assert.AreEqual(2, filteredResult.Count);
        }

        [TestMethod]
        public async Task Test_GetReports_Other_User_Device_Filter_Failed()
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
            _deviceIds.Add(deviceId);

            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Followers", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "Private", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            // Act
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () =>
                await _reportLib.GetReports(accessToken2, [deviceId], descending: false, pageSize: 5, page: 1, userId: _userId));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, requestFailedException.ResponseMessage.StatusCode);
        }

        [TestMethod]
        public async Task Test_GetReports_EndDate()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await _reportLib.CreateSampleReports(_webSocketUri, secret, _accessToken, deviceId, "Test Report", "All", 5, TimeSpan.FromSeconds(1));
            (JsonArray result, _) = await _reportLib.GetReports(_accessToken, [deviceId], descending: true, pageSize: 5, page: 1);

            // Act
            DateTime endTime = result[0].AsObject()["endTime"].GetValue<DateTime>();
            (JsonArray filteredResult, _) = await _reportLib.GetReports(_accessToken, [deviceId], descending: false, endTime: endTime.AddSeconds(-1), pageSize: 5, page: 1);

            // Assert
            Assert.IsNotEmpty(filteredResult);
            Assert.AreNotEqual(filteredResult[0]["id"].GetValue<string>(), result[0]["id"].GetValue<string>());
            Assert.AreNotEqual(result.Count, filteredResult.Count, "Filtered result should not contain the first report");
        }

        [TestMethod]
        public async Task Test_GetReports_Device()
        {
            // Arrange

            // CleanUp previous devices
            await CleanUpDevices();

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            JsonObject device2 = await _deviceLib.CreateDevice(_accessToken, "TestDevice2");
            Guid device2Id = Guid.Parse(device2["deviceId"].GetValue<string>());
            string secret2 = device2["secret"].GetValue<string>();
            _deviceIds.Add(device2Id);

            List<Task> tasks =
                [
                _reportLib.CreateSampleReports(_webSocketUri, secret, _accessToken, deviceId, "Test Report", "All", 5, TimeSpan.FromSeconds(2)),
                _reportLib.CreateSampleReports(_webSocketUri, secret2, _accessToken, device2Id, "Test Report", "All",6, TimeSpan.FromSeconds(2))
                ];
            await Task.WhenAll(tasks);

            // Act
            (JsonArray allReports, _) = await _reportLib.GetReports(_accessToken, [], descending: true, pageSize: 20, page: 1);
            (JsonArray allReportsFiltered, _) = await _reportLib.GetReports(_accessToken, [deviceId, device2Id], descending: true, pageSize: 20, page: 1);
            (JsonArray device1Reports, _) = await _reportLib.GetReports(_accessToken, [deviceId], descending: true, pageSize: 20, page: 1);
            (JsonArray device2Reports, _) = await _reportLib.GetReports(_accessToken, [device2Id], descending: true, pageSize: 20, page: 1);

            // Assert
            Assert.AreEqual(11, allReports.Count, "All reports should contain 11 entries");
            Assert.AreEqual(11, allReportsFiltered.Count, "Filtered reports should contain 11 entries");
            Assert.AreEqual(5, device1Reports.Count, "Device 1 reports should contain 5 entries");
            Assert.AreEqual(6, device2Reports.Count, "Device 2 reports should contain 6 entries");
        }

        [TestMethod]
        [DataRow("abc")]
        [DataRow("Test123")]
        [DataRow("Valid-Name")]
        [DataRow("Name With Spaces")]
        [DataRow("A1-B2 C3")]
        [DataRow("SimpleName")]
        [DataRow("Name-Name-Name")]
        [DataRow("Name With-Mixed Characters")]
        [DataRow("aB3")]
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZ123456")]
        public async Task Test_Update_Report_Valid_Name(string newName)
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            Guid reportId = Guid.Parse(createdReport["id"].GetValue<string>());

            // Act
            await _reportLib.UpdateReport(_accessToken, reportId, name: newName);

            // Assert
            JsonObject updatedReport = await _reportLib.GetReport(_accessToken, reportId);
            Assert.AreEqual(newName, updatedReport["name"].GetValue<string>());
        }

        [TestMethod]
        [DataRow("a")]
        [DataRow("ab")]
        [DataRow("thisstringiswaytoolongtobevalidbecauseitexceeds32characters_andnowweareaddingmoredatatofilluptoexactlyonehundredtwentyeight1")]
        [DataRow("name_with_underscore")]
        [DataRow("name.with.dot")]
        [DataRow("name@domain")]
        [DataRow("namé")]
        [DataRow("na🚀me")]
        [DataRow("na\tme")]
        [DataRow("na\nme")]
        [DataRow("name!")]
        [DataRow("na#me")]
        public async Task Test_Update_Report_Invalid_Name(string invalidNewName)
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            Guid reportId = Guid.Parse(createdReport["id"].GetValue<string>());

            // Act
            RequestFailedException exception = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.UpdateReport(_accessToken, reportId, name: invalidNewName));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, exception.ResponseMessage.StatusCode, "Expected BadRequest for invalid report name");
        }

        [TestMethod]
        [DataRow("All")]
        [DataRow("Followers")]
        [DataRow("Private")]
        public async Task Test_Update_Report_Valid_Visibility(string newVisibility)
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            Guid reportId = Guid.Parse(createdReport["id"].GetValue<string>());

            // Act
            await _reportLib.UpdateReport(_accessToken, reportId, visibilityType: newVisibility);

            // Assert
            JsonObject updatedReport = await _reportLib.GetReport(_accessToken, reportId);
            Assert.AreEqual(newVisibility, updatedReport["visibilityType"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_Update_Report_Invalid_Visibility()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, "Test Report", "All", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            Guid reportId = Guid.Parse(createdReport["id"].GetValue<string>());

            // Act
            string invalidNewVisibility = "test";
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _reportLib.UpdateReport(_accessToken, reportId, visibilityType: invalidNewVisibility));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, requestFailedException.ResponseMessage.StatusCode);
        }
    }
}
