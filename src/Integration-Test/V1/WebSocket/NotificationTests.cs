using BackBuddy.Integration_Test.Extensions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Net.WebSockets;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.WebSocket
{
    [TestClass]
    public class NotificationTests
    {

        private static DeviceLib _deviceLib;
        private static string _accessToken;
        private static string _userId;
        private static string _webSocketUri;
        private static FirebaseLib _firebaseLib;
        private static FirestoreLib _firestoreLib;
        private static NotificationLib _notificationLib;

        private readonly static List<Guid> _deviceIds = [];

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _webSocketUri = Environment.GetEnvironmentVariable("E2E_WEBSOCKET_URI") ?? "ws://localhost:8080/";
            _accessToken = Environment.GetEnvironmentVariable("E2E_ACCESS_TOKEN");
            _userId = Environment.GetEnvironmentVariable("E2E_USER_ID");
            Uri baseUri = new(Environment.GetEnvironmentVariable("E2E_BASE_URI") ?? "http://localhost:8080/");
            Uri notificationUri = new(Environment.GetEnvironmentVariable("E2E_NOTIFICATION_URI") ?? "http://localhost:8083/");

            _deviceLib = new DeviceLib(baseUri.ToString());
            _notificationLib = new NotificationLib(notificationUri.ToString());

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

            await _firestoreLib.CleanUp("users");
        }

        [TestMethod]
        public async Task Test_Notification_Threshold()
        {
            // Arrange
            string fcm_token = "fcmToken1_threshold";

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            await _deviceLib.UpdateDevice(_accessToken, deviceId, active: true, threshold: TimeSpan.FromSeconds(10));
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await _firestoreLib.CreateUserObject(_userId, "Test User", [fcm_token]);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
            await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);

            // Max Attempts = 2 because of the secret change offer
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

            await Task.Delay(TimeSpan.FromSeconds(15));

            // Assert
            JsonArray notifications = await _notificationLib.GetNotifications();
            Assert.AreEqual(1, notifications.Count);

            JsonObject notification = notifications[0].AsObject();
            JsonArray tokens = notification["tokens"].AsArray();
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(fcm_token, tokens[0].GetValue<string>());

            // Cleanup
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

    }
}
