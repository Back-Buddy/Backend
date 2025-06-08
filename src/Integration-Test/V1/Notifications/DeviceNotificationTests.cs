using BackBuddy.Integration_Test.Extensions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Net.WebSockets;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Notifications
{
    [TestClass]
    public class DeviceNotificationTests
    {

        private static string _accessToken;
        private static string _userId;
        private static string _webSocketUri;
        private static DeviceLib _deviceLib;
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

        [TestInitialize]
        public async Task TestInitialize()
        {
            await _notificationLib.ClearNotifications();
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
        public async Task Test_Notification_Threshold_Active_Success()
        {
            // Arrange
            string fcm_token = "fcmToken1_threshold";

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            // For Notification the device must be active
            await _deviceLib.UpdateDevice(_accessToken, deviceId, active: true, threshold: TimeSpan.FromSeconds(5));
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

            await Task.Delay(TimeSpan.FromSeconds(10));

            // Assert
            JsonArray notifications = await _notificationLib.GetNotifications();
            Assert.AreEqual(1, notifications.Count, "Notification should be sended because device is active");

            JsonObject notification = notifications[0].AsObject();
            JsonArray tokens = notification["tokens"].AsArray();
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(fcm_token, tokens[0].GetValue<string>());

            // Cleanup
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Notification_Threshold_Not_Active_Success()
        {
            // Arrange
            string fcm_token = "fcmToken1_threshold";

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            await _deviceLib.UpdateDevice(_accessToken, deviceId, active: false, threshold: TimeSpan.FromSeconds(5));
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

            await Task.Delay(TimeSpan.FromSeconds(10));

            // Assert
            JsonArray notifications = await _notificationLib.GetNotifications();
            Assert.AreEqual(0, notifications.Count, "0 Notifications should be send because the device is not active!");

            // Cleanup
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Notification_Threshold_Extended_No_Notification()
        {
            // Arrange
            string fcm_token = "fcmToken_threshold_long";
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            await _deviceLib.UpdateDevice(_accessToken, deviceId, active: true, threshold: TimeSpan.FromSeconds(30));
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await _firestoreLib.CreateUserObject(_userId, "Test User", [fcm_token]);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
            await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

            await Task.Delay(TimeSpan.FromSeconds(10)); // Below the threshold of 30 seconds

            // Assert
            JsonArray notifications = await _notificationLib.GetNotifications();
            Assert.AreEqual(0, notifications.Count, "No notification should be sent due to long threshold");

            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Notification_Threshold_Active_Time_Extreme_Above_Threshold()
        {
            // Arrange
            string fcm_token = "fcmToken1_threshold";

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            // For Notification the device must be active
            await _deviceLib.UpdateDevice(_accessToken, deviceId, active: true, threshold: TimeSpan.FromSeconds(5));
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

            await Task.Delay(TimeSpan.FromSeconds(30));

            // Assert
            JsonArray notifications = await _notificationLib.GetNotifications();
            Assert.AreEqual(1, notifications.Count, "Notification should be sended because device is active");

            JsonObject notification = notifications[0].AsObject();
            JsonArray tokens = notification["tokens"].AsArray();
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(fcm_token, tokens[0].GetValue<string>());

            // Cleanup
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Notification_Threshold_Active_Multiple_Tokens_Success()
        {
            // Arrange
            string fcm_token = "fcmToken1_threshold";
            string fcm_token2 = "fcmToken2_threshold";
            List<string> fcm_tokens = [fcm_token, fcm_token2];

            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());
            // For Notification the device must be active
            await _deviceLib.UpdateDevice(_accessToken, deviceId, active: true, threshold: TimeSpan.FromSeconds(5));

            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await _firestoreLib.CreateUserObject(_userId, "Test User", fcm_tokens);

            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            // Act
            JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
            await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);

            // Max Attempts = 2 because of the secret change offer
            await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

            await Task.Delay(TimeSpan.FromSeconds(10));

            // Assert
            JsonArray notifications = await _notificationLib.GetNotifications();
            Assert.AreEqual(1, notifications.Count, "Notification should be sended because device is active");

            JsonObject notification = notifications[0].AsObject();
            JsonArray tokens = notification["tokens"].AsArray();
            Assert.AreEqual(2, tokens.Count);
            Assert.IsTrue(tokens.Any(token => token.GetValue<string>() == fcm_token), $"Tokens({string.Join(';', tokens)}) must be contain {fcm_token}");
            Assert.IsTrue(tokens.Any(token => token.GetValue<string>() == fcm_token2), $"Tokens({string.Join(';', tokens)}) must be contain {fcm_token2}");

            // Cleanup
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }

        [TestMethod]
        public async Task Test_Notification_No_Notification_Threshold_Not_Reached()
        {
            // Arrange
            JsonObject device = await _deviceLib.CreateDevice(_accessToken, "TestDevice");
            Guid deviceId = Guid.Parse(device["deviceId"].GetValue<string>());

            // For Notification the device must be active
            await _deviceLib.UpdateDevice(_accessToken, deviceId, active: true, threshold: TimeSpan.FromSeconds(5));
            string secret = device["secret"].GetValue<string>();
            _deviceIds.Add(deviceId);

            await DeviceLogLib.CreateSampleLogs(_webSocketUri, secret, 1, delay: TimeSpan.FromMilliseconds(500));

            // Act
            await Task.Delay(TimeSpan.FromSeconds(10));

            // Assert
            JsonArray notifications = await _notificationLib.GetNotifications();
            Assert.AreEqual(0, notifications.Count, "No notification should be send!");
        }
    }
}
