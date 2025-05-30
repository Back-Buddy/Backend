using BackBuddy.Integration_Test.Extensions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Net.WebSockets;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.WebSocket
{
    [TestClass]
    public class ConnectedDeviceTest
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
        public async Task Test_Connected()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Act
            JsonObject response = await _deviceLib.GetDevice(_accessToken, deviceId);

            // Assert
            Assert.IsTrue(response["online"].GetValue<bool>());

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Not_Connected()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            _deviceIds.Add(deviceId);

            // Act
            JsonObject response = await _deviceLib.GetDevice(_accessToken, deviceId);

            // Assert
            Assert.IsFalse(response["online"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_Not_Connect_Disconnect()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);

            // Act
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
            JsonObject response = await _deviceLib.GetDevice(_accessToken, deviceId);

            // Assert
            Assert.IsFalse(response["online"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_Network_Error()
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
            clientWebSocket.Dispose(); // Simulate network error
            await Task.Delay(1000); // Wait for the server to detect the disconnection
            JsonObject response = await _deviceLib.GetDevice(_accessToken, deviceId);

            // Assert
            Assert.IsFalse(response["online"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_Invalid_Payload()
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
            JsonObject invalidPayload = new()
            {
                ["invalidKey"] = "invalidValue"
            };
            await clientWebSocket.SendAsync(invalidPayload, int.MaxValue, CancellationToken.None);
            await Task.Delay(1000); // Wait for the server to detect the disconnection
            JsonObject response = await _deviceLib.GetDevice(_accessToken, deviceId);

            // Assert False because invalid payload => close connection
            Assert.IsFalse(response["online"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_Invalid_Payload_MessageType()
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
            JsonObject invalidPayload = new()
            {
                ["MessageType"] = "sadsadsad"
            };
            await clientWebSocket.SendAsync(invalidPayload, int.MaxValue, CancellationToken.None);
            await Task.Delay(1000); // Wait for the server to detect the disconnection
            JsonObject response = await _deviceLib.GetDevice(_accessToken, deviceId);

            // Assert False because invalid payload => close connection
            Assert.IsFalse(response["online"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_Invalid_Secret()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            string secret = "testSecret";
            _deviceIds.Add(deviceId);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);

            // Act
            await Assert.ThrowsExactlyAsync<WebSocketException>(async () => await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None));
            JsonObject response = await _deviceLib.GetDevice(_accessToken, deviceId);

            // Assert | False because invalid payload => close connection
            Assert.IsFalse(response["online"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_After_Send_Correct_Message()
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
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

            JsonObject response = await _deviceLib.GetDevice(_accessToken, deviceId);

            // Assert
            Assert.IsTrue(response["online"].GetValue<bool>());

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Long_Connected()
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
            await Task.Delay(TimeSpan.FromSeconds(35)); // Simulate a long connection
            JsonObject response = await _deviceLib.GetDevice(_accessToken, deviceId);

            // Assert
            Assert.IsTrue(response["online"].GetValue<bool>());

            // Clean up
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }
    }
}
