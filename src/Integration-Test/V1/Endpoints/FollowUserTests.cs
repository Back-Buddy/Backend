using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Net;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class FollowUserTests
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
        public async Task Test_FollowUser_Valid()
        {
            // Act
            await _userLib.FollowUser(_accessToken, _userId2);

            // Assert
            (JsonArray following, _) = await _userLib.GetFollowing(_accessToken, _userId);
            (JsonArray followers, _) = await _userLib.GetFollowers(_accessToken, _userId2);

            Assert.IsNotNull(following);
            Assert.IsNotNull(followers);
            Assert.AreEqual(1, following.Count);
            Assert.AreEqual(1, followers.Count);
            Assert.AreEqual(_userId2, following[0].AsObject()["userId"].GetValue<string>());
            Assert.AreEqual(_userId, followers[0].AsObject()["userId"].GetValue<string>());

            Assert.IsNull(following[0].AsObject()["followers"]);
            Assert.IsNull(followers[0].AsObject()["following"]);
        }

        [TestMethod]
        public async Task Test_FollowUser_Valid_ExpandType_Relations()
        {
            // Act
            await _userLib.FollowUser(_accessToken, _userId2);

            // Assert
            (JsonArray following, _) = await _userLib.GetFollowing(_accessToken, _userId, expandType: "Relations");
            (JsonArray followers, _) = await _userLib.GetFollowers(_accessToken, _userId2, expandType: "Relations");

            Assert.IsNotNull(following);
            Assert.IsNotNull(followers);
            Assert.AreEqual(1, following.Count);
            Assert.AreEqual(1, followers.Count);

            Assert.AreEqual(_userId2, following[0].AsObject()["userId"].GetValue<string>());
            Assert.AreEqual(_userId, followers[0].AsObject()["userId"].GetValue<string>());

            Assert.AreEqual(1, following[0].AsObject()["followers"].GetValue<long>());
            Assert.AreEqual(1, followers[0].AsObject()["following"].GetValue<long>());
        }

        [TestMethod]
        public async Task Test_FollowUser_Self()
        {
            // Act
            RequestFailedException exception = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _userLib.FollowUser(_accessToken, _userId));

            // Assert
            (JsonArray following, _) = await _userLib.GetFollowing(_accessToken, _userId);

            Assert.IsFalse(exception.ResponseMessage.IsSuccessStatusCode);
            Assert.AreEqual(HttpStatusCode.BadRequest, exception.ResponseMessage.StatusCode);

            Assert.AreEqual(0, following.Count);
        }

        [TestMethod]
        public async Task Test_FollowUser_Invalid_AlreadyFollowing()
        {
            // Arrange
            await _userLib.FollowUser(_accessToken, _userId2);

            // Act
            RequestFailedException exception = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _userLib.FollowUser(_accessToken, _userId2));

            // Assert
            Assert.IsFalse(exception.ResponseMessage.IsSuccessStatusCode);
            Assert.AreEqual(HttpStatusCode.Conflict, exception.ResponseMessage.StatusCode);
        }

        [TestMethod]
        public async Task Test_FollowUser_Invalid_UnknownUser()
        {
            // Act
            RequestFailedException exception = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _userLib.FollowUser(_accessToken, Guid.CreateVersion7().ToString("N")));

            // Assert
            Assert.IsFalse(exception.ResponseMessage.IsSuccessStatusCode);
            Assert.AreEqual(HttpStatusCode.NotFound, exception.ResponseMessage.StatusCode);

            (JsonArray following, _) = await _userLib.GetFollowing(_accessToken, _userId);
            Assert.AreEqual(0, following.Count);
        }

        [TestMethod]
        public async Task Test_FollowUser_Following_Pagination()
        {
            // Arrange
            for (int i = 0; i < 12; i++)
            {
                string userId = Guid.CreateVersion7().ToString("N");
                await _firestoreLib.CreateUserObject(userId, $"TestUser{i + 3}", []);
                await _userLib.FollowUser(_accessToken, userId);
            }

            // Act
            (JsonArray following, bool hasMoreEntries) = await _userLib.GetFollowing(_accessToken, _userId, page: 1, size: 5);
            Assert.IsTrue(hasMoreEntries);
            Assert.AreEqual(5, following.Count);

            (JsonArray followingPage2, bool hasMoreEntriesPage2) = await _userLib.GetFollowing(_accessToken, _userId, page: 2, size: 5);
            Assert.IsTrue(hasMoreEntriesPage2);
            Assert.AreEqual(5, followingPage2.Count);

            (JsonArray followingPage3, bool hasMoreEntriesPage3) = await _userLib.GetFollowing(_accessToken, _userId, page: 3, size: 5);
            Assert.IsFalse(hasMoreEntriesPage3);
            Assert.AreEqual(2, followingPage3.Count);
        }

        [TestMethod]
        public async Task Test_FollowUser_Followers_Pagination()
        {
            // Arrange
            for (int i = 0; i < 12; i++)
            {
                await _firebaseLib.RegisterUserAsync($"test{i}@gmail.com", "stringG.1212"); //NOT A REAL SECRET
                FirebaseDto.FirebaseLoginResponseDto loginResponse = await _firebaseLib.SignInUserAsync($"test{i}@gmail.com", "stringG.1212"); //NOT A REAL SECRET
                string userId = loginResponse.LocalId;
                string accessToken = loginResponse.IdToken;

                await _firestoreLib.CreateUserObject(userId, $"TestUser{i + 3}", []);
                await _userLib.FollowUser(accessToken, _userId);

                await _firebaseLib.DeleteUserAsync(userId);
            }

            // Act
            (JsonArray following, bool hasMoreEntries) = await _userLib.GetFollowers(_accessToken, _userId, page: 1, size: 5);
            Assert.IsTrue(hasMoreEntries);
            Assert.AreEqual(5, following.Count);

            (JsonArray followingPage2, bool hasMoreEntriesPage2) = await _userLib.GetFollowers(_accessToken, _userId, page: 2, size: 5);
            Assert.IsTrue(hasMoreEntriesPage2);
            Assert.AreEqual(5, followingPage2.Count);

            (JsonArray followingPage3, bool hasMoreEntriesPage3) = await _userLib.GetFollowers(_accessToken, _userId, page: 3, size: 5);
            Assert.IsFalse(hasMoreEntriesPage3);
            Assert.AreEqual(2, followingPage3.Count);
        }

        [TestMethod]
        public async Task Test_FollowUser_Delete()
        {
            // Act & Assert
            await _userLib.FollowUser(_accessToken, _userId2);

            (JsonArray following, _) = await _userLib.GetFollowing(_accessToken, _userId);
            (JsonArray followers, _) = await _userLib.GetFollowers(_accessToken, _userId2);
            Assert.AreEqual(1, following.Count);
            Assert.AreEqual(1, followers.Count);

            await _userLib.DeleteUser(_accessToken);

            (JsonArray following2, _) = await _userLib.GetFollowing(_accessToken, _userId);
            (JsonArray followers2, _) = await _userLib.GetFollowers(_accessToken, _userId2);
            Assert.AreEqual(0, following2.Count);
            Assert.AreEqual(0, followers2.Count);
        }
    }
}
