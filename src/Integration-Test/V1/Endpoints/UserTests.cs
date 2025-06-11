using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class UserTests
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
        public async Task Test_SearchUser_Full_Hit()
        {
            // Arrange
            string searchTerm = "test";
            int limit = 10;

            await _firestoreLib.CreateUserObject(_userId, searchTerm, []);

            // Act
            JsonArray result = await _userLib.SearchUser(_accessToken, searchTerm, limit);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(_userId, result[0].AsValue().GetValue<string>());
        }

        [TestMethod]
        public async Task Test_SearchUser_StartsWith()
        {
            // Arrange
            string searchTerm = "test";
            int limit = 10;

            await _firestoreLib.CreateUserObject(_userId, searchTerm, []);

            // Act & Assert

            for (int i = 0; i < searchTerm.Length; i++)
            {
                string partialSearchTerm = searchTerm[..(i + 1)];
                JsonArray result = await _userLib.SearchUser(_accessToken, partialSearchTerm, limit);
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(_userId, result[0].AsValue().GetValue<string>());
            }
        }

        [TestMethod]
        public async Task Test_SearchUser_No_Hit()
        {
            // Arrange
            string searchTerm = "nic";
            int limit = 10;

            await _firestoreLib.CreateUserObject(_userId, "test", []);

            // Act
            JsonArray result = await _userLib.SearchUser(_accessToken, searchTerm, limit);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task Test_SearchUser_Multi_User()
        {
            // Arrange
            string searchTerm = "test";
            int limit = 10;

            for (int i = 0; i < 5; i++)
            {
                await _firestoreLib.CreateUserObject(Guid.NewGuid().ToString("N"), $"test{i}", []);
            }

            // Act
            JsonArray result = await _userLib.SearchUser(_accessToken, searchTerm, limit);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.Count);
        }

        [TestMethod]
        public async Task Test_SearchUser_Limit()
        {
            // Arrange
            string searchTerm = "test";
            int limit = 10;

            for (int i = 0; i < limit + 5; i++)
            {
                await _firestoreLib.CreateUserObject(Guid.NewGuid().ToString("N"), $"test{i}", []);
            }

            // Act
            JsonArray result = await _userLib.SearchUser(_accessToken, searchTerm, limit);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(limit, result.Count);
        }

        [TestMethod]
        public async Task Test_SearchUser_CaseInsensitive()
        {
            // Arrange
            string searchTerm = "TeSt";
            int limit = 10;
            await _firestoreLib.CreateUserObject(_userId, "test", []);

            // Act
            JsonArray result = await _userLib.SearchUser(_accessToken, searchTerm, limit);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(_userId, result[0].AsValue().GetValue<string>());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(101)]

        public async Task Test_SearchUser_InvalidLimit(int limit)
        {
            // Arrange
            string searchTerm = "test";

            await _firestoreLib.CreateUserObject(_userId, searchTerm, []);

            // Act
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _userLib.SearchUser(_accessToken, searchTerm, limit));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, requestFailedException.ResponseMessage.StatusCode);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("test@!%")]
        public async Task Test_SearchUser_Invalid_SearchTerm(string searchTerm)
        {
            // Arrange
            int limit = 10;

            await _firestoreLib.CreateUserObject(_userId, "test", []);

            // Act
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _userLib.SearchUser(_accessToken, searchTerm, limit));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, requestFailedException.ResponseMessage.StatusCode);
        }

        [TestMethod]
        [DataRow("test ")]
        [DataRow(" test")]
        public async Task Test_SearchUser_With_Space(string searchTerm)
        {
            // Arrange
            int limit = 10;
            await _firestoreLib.CreateUserObject(_userId, "test", []);

            // Act
            JsonArray result = await _userLib.SearchUser(_accessToken, searchTerm, limit);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(_userId, result[0].AsValue().GetValue<string>());
        }
    }
}
