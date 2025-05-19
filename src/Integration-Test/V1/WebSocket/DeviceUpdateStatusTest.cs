using BackBuddy.Integration_Test.Extensions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Net.WebSockets;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.WebSocket
{
    [TestClass]
    public class DeviceUpdateStatusTest
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
        public async Task Test_Update_Success()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
            DateTime sendSitTime = DateTime.UtcNow;
            await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);

            // Max Attempts = 2 because of the secret change offer
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

            JsonObject standingStatus = DeviceLib.CreateUpdateStatus("Standing");
            DateTime sendStandTime = DateTime.UtcNow;
            await clientWebSocket.SendAsync(standingStatus, int.MaxValue, CancellationToken.None);

            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);

            // Assert
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId);
            Assert.AreEqual(1, logs.Count);
            JsonObject log = logs[0].AsObject();

            Assert.IsTrue(log["startTime"].GetValue<DateTime>() >= sendSitTime, "StartTime must be greater than the send time");
            Assert.IsTrue(log["endTime"].GetValue<DateTime>() >= sendStandTime, "EndTime must be greater than the send time");
            Assert.IsTrue(log["endTime"].GetValue<DateTime>() >= log["startTime"].GetValue<DateTime>(), "EndTime must be greater than StartTime");
            Assert.IsTrue((log["endTime"].GetValue<DateTime>() - sendStandTime) <= TimeSpan.FromSeconds(15), "EndTime and send time small difference");
            Assert.IsTrue((log["startTime"].GetValue<DateTime>() - sendSitTime) <= TimeSpan.FromSeconds(15), "StartTime and send time small difference");
            Assert.AreEqual("Sit", log["logType"].GetValue<string>());

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Update_Currently_Sit()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
            await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);

            // Max Attempts = 2 because of the secret change offer
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

            JsonObject sittingStatus2 = DeviceLib.CreateUpdateStatus("Sitting");
            await clientWebSocket.SendAsync(sittingStatus2, int.MaxValue, CancellationToken.None);

            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);

            // Assert
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId);
            Assert.AreEqual(1, logs.Count);
            JsonObject log = logs[0].AsObject();

            Assert.AreEqual("Error", log["logType"].GetValue<string>());

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Update_Double_Standing()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            JsonObject standingStatus1 = DeviceLib.CreateUpdateStatus("Standing");
            await clientWebSocket.SendAsync(standingStatus1, int.MaxValue, CancellationToken.None);

            // Max Attempts = 2 because of the secret change offer
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

            JsonObject standingStatus2 = DeviceLib.CreateUpdateStatus("Standing");
            await clientWebSocket.SendAsync(standingStatus2, int.MaxValue, CancellationToken.None);

            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);

            // Assert => No log should be created
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId);
            Assert.AreEqual(0, logs.Count);

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Update_Reverse_State()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            JsonObject standingStatus = DeviceLib.CreateUpdateStatus("Standing");
            await clientWebSocket.SendAsync(standingStatus, int.MaxValue, CancellationToken.None);

            // Max Attempts = 2 because of the secret change offer
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

            JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
            await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);

            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);

            // Assert => No log should be created
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId);
            Assert.AreEqual(0, logs.Count);

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Update_Double_Send_ACK_Failure()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
            await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);

            // Ignore ACK Failed
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

            await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);

            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);

            JsonObject standingStatus = DeviceLib.CreateUpdateStatus("Standing");
            await clientWebSocket.SendAsync(standingStatus, int.MaxValue, CancellationToken.None);

            // Ignore ACK Failed
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);

            await clientWebSocket.SendAsync(standingStatus, int.MaxValue, CancellationToken.None);

            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);

            // Assert
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId);
            Assert.AreEqual(2, logs.Count); // Two logs should be created because double sit status = failure

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Update_Invalid_Status()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            JsonObject invalidStatus = DeviceLib.CreateUpdateStatus("Jumping");
            await clientWebSocket.SendAsync(invalidStatus, int.MaxValue, CancellationToken.None);

            // Assert
            (JsonArray logs, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId);
            Assert.AreEqual(0, logs.Count, "No logs should be created for an invalid status");

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Update_Parallel_Device_Status()
        {
            // Arrange
            JsonObject device1 = await _deviceLib.CreateDevice(_accessToken, "TestDevice 1");
            Guid deviceId1 = Guid.Parse(device1["deviceId"].GetValue<string>());
            string secret1 = device1["secret"].GetValue<string>();
            _deviceIds.Add(deviceId1);

            JsonObject device2 = await _deviceLib.CreateDevice(_accessToken, "TestDevice 2");
            Guid deviceId2 = Guid.Parse(device2["deviceId"].GetValue<string>());
            string secret2 = device2["secret"].GetValue<string>();
            _deviceIds.Add(deviceId2);

            using ClientWebSocket clientWebSocket1 = new();
            using ClientWebSocket clientWebSocket2 = new();
            clientWebSocket1.Options.AddSubProtocol(secret1);
            clientWebSocket2.Options.AddSubProtocol(secret2);
            await clientWebSocket1.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);
            await clientWebSocket2.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            Task task1 = clientWebSocket1.SendAsync(DeviceLib.CreateUpdateStatus("Sitting"), int.MaxValue, CancellationToken.None);
            Task task2 = clientWebSocket2.SendAsync(DeviceLib.CreateUpdateStatus("Sitting"), int.MaxValue, CancellationToken.None);
            await Task.WhenAll(task1, task2);

            task1 = clientWebSocket1.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);
            task2 = clientWebSocket2.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);
            await Task.WhenAll(task1, task2);


            task1 = clientWebSocket1.SendAsync(DeviceLib.CreateUpdateStatus("Standing"), int.MaxValue, CancellationToken.None);
            task2 = clientWebSocket2.SendAsync(DeviceLib.CreateUpdateStatus("Standing"), int.MaxValue, CancellationToken.None);
            await Task.WhenAll(task1, task2);

            task1 = clientWebSocket1.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);
            task2 = clientWebSocket2.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);
            await Task.WhenAll(task1, task2);

            // Assert
            (JsonArray logsDevice1, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId1);
            Assert.AreEqual(1, logsDevice1.Count, "Only one log should be created");

            (JsonArray logsDevice2, _) = await _deviceLogLib.GetLogs(_accessToken, deviceId2);
            Assert.AreEqual(1, logsDevice2.Count, "Only one log should be created");

            // Clean up
            await clientWebSocket1.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
            await clientWebSocket2.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

    }
}
