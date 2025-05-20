using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class DeviceLogTests
    {
        private static DeviceLogLib _deviceLogLib;
        private static DeviceLib _deviceLib;
        private static string _accessToken;
        private static string _userId;
        private static string _webSocketUri;
        private static FirebaseLib _firebaseLib;

        private readonly static List<Guid> _deviceIds = [];

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _webSocketUri = Environment.GetEnvironmentVariable("E2E_WEBSOCKET_URI") ?? "ws://localhost:8080/";
            _accessToken = Environment.GetEnvironmentVariable("E2E_ACCESS_TOKEN");
            _userId = Environment.GetEnvironmentVariable("E2E_USER_ID");
            Uri baseUri = new(Environment.GetEnvironmentVariable("E2E_BASE_URI") ?? "http://localhost:8080/");

            _deviceLib = new DeviceLib(baseUri.ToString());
            _deviceLogLib = new DeviceLogLib(baseUri.ToString());

            if (_accessToken == null)
            {
                _firebaseLib = new("http://localhost:9099/identitytoolkit.googleapis.com/v1/", "change-me");
                await _firebaseLib.RegisterUserAsync("test@gmail.com", "stringG.1212"); //NOT A REAL SECRET
                FirebaseDto.FirebaseLoginResponseDto loginResponse = await _firebaseLib.SignInUserAsync("test@gmail.com", "stringG.1212"); //NOT A REAL SECRET
                _userId = loginResponse.LocalId;
                _accessToken = loginResponse.IdToken;
            }
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static async Task ClassCleanup()
        {
            if (_firebaseLib != null)
            {
                await _firebaseLib.DeleteUserAsync(_userId);
            }
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            foreach (Guid deviceId in _deviceIds)
            {
                await _deviceLib.DeleteDevice(_accessToken, deviceId);
            }
            _deviceIds.Clear();
        }

        [TestMethod]
        public async Task Test_GetLogs_Empty()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "EmptyDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            // Act
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId);

            // Assert
            Assert.IsNotNull(logs);
            Assert.AreEqual(0, logs.Count, "The log list should be empty");
        }

        [TestMethod]
        public async Task Test_GetLogs_All_Success()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 5, 0);

            // Act
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId);

            // Assert
            Assert.IsNotNull(logs);
            Assert.AreEqual(5, logs.Count);
            Assert.IsTrue(logs.All(x => x.AsObject()["logType"].GetValue<string>() == "Sit"), "All logs should be of type 'Sit'");
        }

        [TestMethod]
        public async Task Test_GetLogs_All_Error()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 0, 5);

            // Act
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId);

            // Assert
            Assert.IsNotNull(logs);
            Assert.AreEqual(5, logs.Count);
            Assert.IsTrue(logs.All(x => x.AsObject()["logType"].GetValue<string>() == "Error"), "All logs should be of type 'Error'");
        }

        [TestMethod]
        [DataRow("Sit")]
        [DataRow("Error")]
        public async Task Test_GetLogs_Mixed_Filtered(string logType)
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 5, 5);

            // Act
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId, logType: logType);

            // Assert
            Assert.IsNotNull(logs);
            Assert.AreEqual(5, logs.Count);
            Assert.IsTrue(logs.All(x => x.AsObject()["logType"].GetValue<string>() == logType), $"All logs should be of type '{logType}'");
        }

        [TestMethod]
        public async Task Test_GetLogs_Mixed_Sorted_Descending()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 5, 5);

            // Act
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId, descending: true);

            // Assert
            Assert.IsNotNull(logs);
            Assert.AreEqual(10, logs.Count);
            Assert.IsTrue(logs[0].AsObject()["startTime"].GetValue<DateTime>() >= logs[1].AsObject()["startTime"].GetValue<DateTime>(), "Logs should be sorted by start time in descending order");
        }

        [TestMethod]
        public async Task Test_GetLogs_Mixed_Sorted_Ascending()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 5, 5);

            // Act
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId, descending: false);

            // Assert
            Assert.IsNotNull(logs);
            Assert.AreEqual(10, logs.Count);
            Assert.IsTrue(logs[0].AsObject()["startTime"].GetValue<DateTime>() <= logs[1].AsObject()["startTime"].GetValue<DateTime>(), "Logs should be sorted by start time in ascending order");
        }

        [TestMethod]
        public async Task Test_GetLogs_Mixed_Pagination()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 5, 6);

            // Act & Assert
            JsonArray logs;
            bool hasMoreResults;
            (logs, hasMoreResults) = await _deviceLogLib.GetLogs(_accessToken, deviceId, descending: false, page: 1, pageSize: 5);

            Assert.AreEqual(5, logs.Count);
            Assert.IsTrue(hasMoreResults, "There should be more results available");

            (logs, hasMoreResults) = await _deviceLogLib.GetLogs(_accessToken, deviceId, descending: false, page: 2, pageSize: 5);
            Assert.AreEqual(5, logs.Count);
            Assert.IsTrue(hasMoreResults, "There should be more results available");

            (logs, hasMoreResults) = await _deviceLogLib.GetLogs(_accessToken, deviceId, descending: false, page: 3, pageSize: 5);
            Assert.AreEqual(1, logs.Count);
            Assert.IsFalse(hasMoreResults, "There should be not more results available");
        }

        [TestMethod]
        public async Task Test_GetLogs_Mixed_Filter_StartDate()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 5, delay: TimeSpan.FromSeconds(3));

            // Act
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId, descending: false);
            JsonObject firstLog = logs[0].AsObject();
            DateTime startDate = firstLog["startTime"].GetValue<DateTime>().AddSeconds(1);

            (JsonArray filteredLogs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId, startTime: startDate);

            // Assert
            Assert.IsNotNull(filteredLogs);
            Assert.AreEqual(4, filteredLogs.Count, "Filtered logs should be 4");
            Assert.IsFalse(filteredLogs.Any(x => x.AsObject()["id"].GetValue<string>() == firstLog["id"].GetValue<string>()), "Fillered logs not contains firstlog");
        }

        [TestMethod]
        public async Task Test_GetLogs_Mixed_Filter_EndDate()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 5, delay: TimeSpan.FromSeconds(3));

            // Act
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId, descending: true);
            JsonObject lastLog = logs[0].AsObject();
            DateTime endDate = lastLog["endTime"].GetValue<DateTime>().AddSeconds(-1);

            (JsonArray filteredLogs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId, endTime: endDate);

            // Assert
            Assert.IsNotNull(filteredLogs);
            Assert.AreEqual(4, filteredLogs.Count, "Filtered logs should be 4");
            Assert.IsFalse(filteredLogs.Any(x => x.AsObject()["id"].GetValue<string>() == lastLog["id"].GetValue<string>()), "Fillered logs not contains lastlog");
        }

    }
}
