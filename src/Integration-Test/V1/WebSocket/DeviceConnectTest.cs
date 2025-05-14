using BackBuddy.Integration_Test.Extensions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Net.WebSockets;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.WebSocket
{
    [TestClass]
    public class DeviceConnectTest
    {
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
        public async Task Test_Connect()
        {
            // Arrange
            string deviceName = "TestDevice";
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, deviceName);
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            // Act
            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Assert
            Assert.AreEqual(WebSocketState.Open, clientWebSocket.State);

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Connect_Unauthorized_Invalid_Secret()
        {
            // Arrange
            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol("blablabla");

            // Act & Assert
            WebSocketException webSocketException = await Assert.ThrowsExactlyAsync<WebSocketException>(async () => await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None));
            Assert.IsTrue(webSocketException.Message.Contains("401"));
        }

        [TestMethod]
        public async Task Test_Connect_Unauthorized_No_Secret()
        {
            // Arrange
            using ClientWebSocket clientWebSocket = new();

            // Act & Assert
            WebSocketException webSocketException = await Assert.ThrowsExactlyAsync<WebSocketException>(async () => await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None));
            Assert.IsTrue(webSocketException.Message.Contains("401"));
        }


        [TestMethod]
        public async Task Test_Connect_New_Secret()
        {
            // Arrange
            string deviceName = "TestDevice";
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, deviceName);
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            // Act
            await Task.Delay(1100);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);

            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Assert
            JsonObject newSecretObj = await clientWebSocket.PollMessage("DeviceNewSecret", 1, CancellationToken.None);

            string newSecret = newSecretObj["Secret"].GetValue<string>();
            JsonObject ackNewSecret = new()
            {
                ["secret"] = newSecret,
                ["messageType"] = "DeviceNewSecretAck"
            };

            await clientWebSocket.SendAsync(ackNewSecret, int.MaxValue, CancellationToken.None);

            // Receive ACK
            JsonObject ackObj = await clientWebSocket.PollMessage("DeviceNewSecretSetAck", 1, CancellationToken.None);
            Assert.AreEqual("DeviceNewSecretSetAck", ackObj["MessageType"].GetValue<string>());

            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);

            // Assert that the new secret works
            using ClientWebSocket clientWebSocketCheck = new();
            clientWebSocketCheck.Options.AddSubProtocol(newSecret);
            await clientWebSocketCheck.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);
            await clientWebSocketCheck.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Connect_New_Secret_Double_ACK()
        {
            // Arrange
            string deviceName = "TestDevice";
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, deviceName);
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            // Act
            await Task.Delay(1100);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);

            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Assert
            JsonObject newSecretObj = await clientWebSocket.PollMessage("DeviceNewSecret", 1, CancellationToken.None);

            string newSecret = newSecretObj["Secret"].GetValue<string>();
            JsonObject ackNewSecret = new()
            {
                ["secret"] = newSecret,
                ["messageType"] = "DeviceNewSecretAck"
            };

            await clientWebSocket.SendAsync(ackNewSecret, int.MaxValue, CancellationToken.None);

            // Receive ACK -> Ignore simulate Network error
            await clientWebSocket.PollMessage("DeviceNewSecretSetAck", 1, CancellationToken.None);

            // Send ACK again
            await clientWebSocket.SendAsync(ackNewSecret, int.MaxValue, CancellationToken.None);

            JsonObject ackObj = await clientWebSocket.PollMessage("DeviceNewSecretSetAck", 1, CancellationToken.None);

            Assert.AreEqual("DeviceNewSecretSetAck", ackObj["MessageType"].GetValue<string>());

            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);

            // Assert that the new secret works
            using ClientWebSocket clientWebSocketCheck = new();
            clientWebSocketCheck.Options.AddSubProtocol(newSecret);
            await clientWebSocketCheck.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);
            await clientWebSocketCheck.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }
    }
}
