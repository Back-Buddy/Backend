using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class GetUserTests
    {
        private static string _accessToken;
        private static string _userId;
        private static UserLib _userLib;
        private static FirebaseLib _firebaseLib;
        private static FirestoreLib _firestoreLib;


        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _accessToken = Environment.GetEnvironmentVariable("E2E_ACCESS_TOKEN");
            _userId = Environment.GetEnvironmentVariable("E2E_USER_ID");
            Uri baseUri = new(Environment.GetEnvironmentVariable("E2E_BASE_URI") ?? "http://localhost:8080/");

            _userLib = new UserLib(baseUri.ToString());

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
        public async Task Test_GetUser_Success()
        {
            // Arrange
            string username = "TestUser";
            await _firestoreLib.CreateUserObject(_userId, username, []);

            // Act
            JsonObject userObj = await _userLib.GetUser(_accessToken, _userId);

            // Assert
            Assert.IsNotNull(userObj);
            Assert.AreEqual(_userId, userObj["userId"].GetValue<string>());
            Assert.AreEqual(username, userObj["username"].GetValue<string>());
            Assert.IsNull(userObj["avatar"]);

            Assert.IsNull(userObj["followers"]);
            Assert.IsNull(userObj["following"]);
        }

        [TestMethod]
        public async Task Test_GetUser_With_Avatar()
        {
            // Arrange
            string username = "TestUser";
            string avatarUrl = "https://example.com/avatar.png";
            await _firestoreLib.CreateUserObject(_userId, username, [], avatarUrl);

            // Act
            JsonObject userObj = await _userLib.GetUser(_accessToken, _userId);

            // Assert
            Assert.IsNotNull(userObj);
            Assert.AreEqual(_userId, userObj["userId"].GetValue<string>());
            Assert.AreEqual(username, userObj["username"].GetValue<string>());
            Assert.AreEqual(avatarUrl, userObj["avatar"].GetValue<string>());

            Assert.IsNull(userObj["followers"]);
            Assert.IsNull(userObj["following"]);
        }

        [TestMethod]
        public async Task Test_GetUser_Expand_Relations()
        {
            // Arrange
            string username = "TestUser";
            await _firestoreLib.CreateUserObject(_userId, username, []);

            // Act
            JsonObject userObj = await _userLib.GetUser(_accessToken, _userId, "Relations");

            // Assert
            Assert.IsNotNull(userObj);
            Assert.AreEqual(_userId, userObj["userId"].GetValue<string>());
            Assert.AreEqual(username, userObj["username"].GetValue<string>());
            Assert.IsNull(userObj["avatar"]);

            Assert.AreEqual(0, userObj["followers"].GetValue<long>());
            Assert.AreEqual(0, userObj["following"].GetValue<long>());
        }
    }
}
