using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class UserDeletedTests
    {
        private static string _accessToken;
        private static string _userId;
        private static UserLib _userLib;
        private static DeviceLib _deviceLib;
        private static FirebaseLib _firebaseLib;
        private static FirestoreLib _firestoreLib;


        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _accessToken = Environment.GetEnvironmentVariable("E2E_ACCESS_TOKEN");
            _userId = Environment.GetEnvironmentVariable("E2E_USER_ID");
            Uri baseUri = new(Environment.GetEnvironmentVariable("E2E_BASE_URI") ?? "http://localhost:8080/");

            _userLib = new UserLib(baseUri.ToString());
            _deviceLib = new DeviceLib(baseUri.ToString());

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
            await _firestoreLib.CleanUpUsers();
        }

        [TestMethod]
        public async Task Test_User_Delete_Device()
        {
            // Arrange
            string username = "TestUser";
            await _firestoreLib.CreateUserObject(_userId, username, []);

            await _deviceLib.CreateSimpleDevice(_accessToken, "TestDevice");

            // Act & Assert
            (JsonArray devicesBeforeDelete, bool _) = await _deviceLib.GetDevices(_accessToken, 1, 10);
            Assert.AreEqual(1, devicesBeforeDelete.Count);

            await _userLib.DeleteUser(_accessToken);
            await Task.Delay(2000); // Wait for deletion (Async operations may take time)

            (JsonArray devicesAfterDelete, bool _) = await _deviceLib.GetDevices(_accessToken, 1, 10);
            Assert.AreEqual(0, devicesAfterDelete.Count);
        }

        [TestMethod]
        public async Task Test_User_Delete_Relations()
        {
            // Arrange
            string username = "TestUser";
            await _firestoreLib.CreateUserObject(_userId, username, []);
            string userId2 = Guid.NewGuid().ToString("N");
            await _firestoreLib.CreateUserObject(userId2, username + "2", []);

            await _userLib.FollowUser(_accessToken, userId2);

            // Act & Assert
            (JsonArray followersBeforeDelete, bool _) = await _userLib.GetFollowers(_accessToken, userId2, "None", 1, 10);
            Assert.AreEqual(1, followersBeforeDelete.Count);

            await _userLib.DeleteUser(_accessToken);
            await Task.Delay(2000); // Wait for deletion (Async operations may take time)

            (JsonArray followersAfterDelete, bool _) = await _userLib.GetFollowers(_accessToken, userId2, "None", 1, 10);
            Assert.AreEqual(0, followersAfterDelete.Count);
        }

        [TestMethod]
        public async Task Test_User_Delete_Invalid_JWT()
        {
            // Arrange
            string jwt = "aW52YWxpZA==.and0.dG9rZW4=";

            // Act
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _userLib.DeleteUser(jwt));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, requestFailedException.ResponseMessage.StatusCode);
        }
    }
}
