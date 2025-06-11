using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Net;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class UnfollowUserTest
    {
        private static string _accessToken;
        private static string _userId;
        private static string _userId2;
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
            await _firestoreLib.CreateUserObject(_userId, "TestUser", []);

            _userId2 = Guid.CreateVersion7().ToString("N");
            await _firestoreLib.CreateUserObject(_userId2, "TestUser2", []);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await _firestoreLib.CleanUpUsers();
            await _userLib.DeleteUser(_accessToken);
        }
        [TestMethod]
        public async Task Test_UnfollowUser_Valid()
        {
            // Arrange
            await _userLib.FollowUser(_accessToken, _userId2);

            (JsonArray following, _) = await _userLib.GetFollowing(_accessToken, _userId);
            (JsonArray followers, _) = await _userLib.GetFollowers(_accessToken, _userId2);

            Assert.AreEqual(1, following.Count);
            Assert.AreEqual(1, followers.Count);

            // Act
            await _userLib.UnfollowUser(_accessToken, _userId2);

            // Assert
            (JsonArray following2, _) = await _userLib.GetFollowing(_accessToken, _userId);
            (JsonArray followers2, _) = await _userLib.GetFollowers(_accessToken, _userId2);

            Assert.AreEqual(0, following2.Count);
            Assert.AreEqual(0, followers2.Count);
        }

        [TestMethod]
        public async Task Test_UnfollowUser_Invalid_NotFollowing()
        {
            // Act
            RequestFailedException exception = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _userLib.UnfollowUser(_accessToken, _userId2));

            // Assert
            Assert.AreEqual(HttpStatusCode.Conflict, exception.ResponseMessage.StatusCode);
        }

        [TestMethod]
        public async Task Test_UnfollowUser_Invalid_UnknownUser()
        {
            // Act
            RequestFailedException exception = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _userLib.UnfollowUser(_accessToken, Guid.CreateVersion7().ToString("N")));

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, exception.ResponseMessage.StatusCode);
        }
    }
}
