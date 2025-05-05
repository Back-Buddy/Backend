using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.WebSocket
{
    [TestClass]
    public class DeviceWebSocketTest
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

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);

            // Act
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);

            await Task.Delay(1100);

            using ClientWebSocket clientWebSocketNew = new();
            clientWebSocketNew.Options.AddSubProtocol(secret);

            await clientWebSocketNew.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Assert
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result = await clientWebSocketNew.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            string rawContent = Encoding.UTF8.GetString(buffer[..result.Count]);
            JsonObject newSecretObj = JsonSerializer.Deserialize<JsonObject>(rawContent);

            string newSecret = newSecretObj["Secret"].GetValue<string>();
            JsonObject ackNewSecret = new()
            {
                ["secret"] = newSecret,
                ["messageType"] = "DeviceNewSecretAck"
            };

            await clientWebSocketNew.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(ackNewSecret.ToJsonString())), WebSocketMessageType.Text, true, CancellationToken.None);
            await clientWebSocketNew.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);

            await Task.Delay(5100); //We need an other callback

            using ClientWebSocket clientWebSocketCheck = new();
            clientWebSocketCheck.Options.AddSubProtocol(newSecret);
            await clientWebSocketCheck.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);
            await clientWebSocketCheck.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }
    }
}
