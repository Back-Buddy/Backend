using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Globalization;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class ReportTests
    {
        private static DeviceLib _deviceLib;
        private static DeviceLogLib _deviceLogLib;
        private static ReportLib _reportLib;
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
            _reportLib = new ReportLib(baseUri.ToString());

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
        public async Task Test_CreateReport_Success()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            TimeSpan sitDuration = TimeSpan.FromSeconds(5);
            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, 0, sitDuration);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;

            // Act
            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, startTime, endTime);

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(deviceId, Guid.Parse(report["deviceId"].GetValue<string>()));
            Assert.AreEqual(startTime, report["startTime"].GetValue<DateTime>());
            Assert.AreEqual(endTime, report["endTime"].GetValue<DateTime>());

            Assert.AreEqual(1, report["usedLogs"].AsArray().Count);

            JsonObject metaData = report["metadata"].AsObject();
            Assert.IsNotNull(metaData);
            Assert.IsTrue(TimeSpan.Parse(metaData["sitTime"].GetValue<string>(), new CultureInfo("en-US")) > TimeSpan.Zero, "Sittime must be greater than 0");
            Assert.IsTrue(Math.Abs((TimeSpan.Parse(metaData["sitTime"].GetValue<string>(), new CultureInfo("en-US")) - sitDuration).TotalSeconds) < 0.5, "Sittime and Sitduration must be equal (Threshold: 0.5)");
        }

        [TestMethod]
        public async Task Test_CreateReport_NoLogs()
        {
            // Arrange 
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            DateTime startTime = DateTime.UtcNow.AddSeconds(-10);
            DateTime endTime = DateTime.UtcNow;

            // Act
            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, startTime, endTime);

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(deviceId, Guid.Parse(report["deviceId"].GetValue<string>()));

            Assert.AreEqual(0, report["usedLogs"].AsArray().Count);
        }

        [TestMethod]
        public async Task Test_CreateReport_MoreLogs()
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
            JsonObject report = await _reportLib.CreateReport(_accessToken, deviceId, startTime, endTime);

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.ContainsKey("id"));
            Assert.AreEqual(deviceId, Guid.Parse(report["deviceId"].GetValue<string>()));

            Assert.AreEqual(10, report["usedLogs"].AsArray().Count);
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
            JsonObject createdReport = await _reportLib.CreateReport(_accessToken, deviceId, startTime, endTime);

            // Act
            JsonObject getReport = await _reportLib.GetReport(_accessToken, Guid.Parse(createdReport["id"].GetValue<string>()));

            // Assert
            Assert.IsNotNull(getReport);
            Assert.IsTrue(getReport.ContainsKey("id"));
            Assert.AreEqual(createdReport["id"].GetValue<string>(), getReport["id"].GetValue<string>());
            Assert.AreEqual(deviceId, Guid.Parse(getReport["deviceId"].GetValue<string>()));
            Assert.AreEqual(startTime.ToString("f"), getReport["startTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(endTime.ToString("f"), getReport["endTime"].GetValue<DateTime>().ToString("f"));
            Assert.AreEqual(1, getReport["usedLogs"].AsArray().Count);
        }

    }
}
