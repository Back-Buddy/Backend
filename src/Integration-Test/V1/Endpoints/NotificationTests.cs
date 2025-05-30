using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class NotificationTests
    {
        private static NotificationLib _notificationLib;
        private static string _accessToken;
        private static string _userId;
        private static FirebaseLib _firebaseLib;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _accessToken = Environment.GetEnvironmentVariable("E2E_ACCESS_TOKEN");
            _userId = Environment.GetEnvironmentVariable("E2E_USER_ID");
            Uri baseUri = new(Environment.GetEnvironmentVariable("E2E_BASE_URI") ?? "http://localhost:8080/");

            _notificationLib = new NotificationLib(baseUri.ToString());

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

        [TestMethod]
        public async Task Test_SetFcmToken_Success()
        {
            // Arrange
            string token = "fcm-test-token-" + Guid.NewGuid().ToString();

            // Act
            await _notificationLib.SetFcmToken(_accessToken, token);

            // Assert
            // No exception means success, as the endpoint returns 204 No Content
        }

        [TestMethod]
        public async Task Test_SetFcmToken_EmptyToken()
        {
            // Arrange
            string token = "";

            // Act & Assert
            await Assert.ThrowsExactlyAsync<RequestFailedException>(async () =>
                await _notificationLib.SetFcmToken(_accessToken, token));
        }

        [TestMethod]
        public async Task Test_UpdateFcmToken_Success()
        {
            // Arrange
            string initialToken = "fcm-test-token-" + Guid.NewGuid().ToString();
            string updatedToken = "fcm-updated-token-" + Guid.NewGuid().ToString();

            // Act
            // Setze zuerst das initiale Token
            await _notificationLib.SetFcmToken(_accessToken, initialToken);

            // Aktualisiere dann mit einem neuen Token (sollte upsert verwenden)
            await _notificationLib.SetFcmToken(_accessToken, updatedToken);

            // Assert
            // Keine Exception bedeutet Erfolg, da der Endpunkt 204 No Content zurückgibt
        }
    }
}
