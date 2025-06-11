using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class RelationTests
    {

        private static string _accessToken;
        private static string _accessToken2;
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

                await _firebaseLib.RegisterUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
                FirebaseDto.FirebaseLoginResponseDto loginResponse2 = await _firebaseLib.SignInUserAsync("test2@gmail.com", "stringG.1212"); //NOT A REAL SECRET
                _userId2 = loginResponse2.LocalId;
                _accessToken2 = loginResponse2.IdToken;

                _firestoreLib = new("http://localhost:8082/", "change-me");
            }
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static async Task ClassCleanup()
        {
            if (_firebaseLib != null)
            {
                await _firebaseLib.DeleteUserAsync(_userId);
                await _firebaseLib.DeleteUserAsync(_userId2);
            }
        }

        [TestInitialize]
        public async Task TestInitialize()
        {
            await _firestoreLib.CreateUserObject(_userId, "TestUser", []);
            await _firestoreLib.CreateUserObject(_userId2, "TestUser2", []);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await _firestoreLib.CleanUpUsers();
            await _userLib.DeleteUser(_accessToken);
        }

        [TestMethod]
        public async Task Test_GetRelationship_Following_Valid()
        {
            // Arrange
            await _userLib.FollowUser(_accessToken, _userId2);

            // Act
            JsonObject relationship = await _userLib.GetRelation(_accessToken, _userId2);

            // Assert
            Assert.IsNotNull(relationship);
            Assert.IsTrue(relationship["isFollowing"].AsValue().GetValue<bool>());
            Assert.IsFalse(relationship["isFollowedBy"].AsValue().GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_GetRelationship_FollowedBy_Valid()
        {
            // Arrange
            await _userLib.FollowUser(_accessToken2, _userId);

            // Act
            JsonObject relationship = await _userLib.GetRelation(_accessToken, _userId2);

            // Assert
            Assert.IsNotNull(relationship);
            Assert.IsFalse(relationship["isFollowing"].AsValue().GetValue<bool>());
            Assert.IsTrue(relationship["isFollowedBy"].AsValue().GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_GetRelationship_Strong_Valid()
        {
            // Arrange
            await _userLib.FollowUser(_accessToken, _userId2);
            await _userLib.FollowUser(_accessToken2, _userId);

            // Act
            JsonObject relationship = await _userLib.GetRelation(_accessToken, _userId2);

            // Assert
            Assert.IsNotNull(relationship);
            Assert.IsTrue(relationship["isFollowing"].AsValue().GetValue<bool>());
            Assert.IsTrue(relationship["isFollowedBy"].AsValue().GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_GetRelationship_Nothing_Valid()
        {
            // Act
            JsonObject relationship = await _userLib.GetRelation(_accessToken, _userId2);

            // Assert
            Assert.IsNotNull(relationship);
            Assert.IsFalse(relationship["isFollowing"].AsValue().GetValue<bool>());
            Assert.IsFalse(relationship["isFollowedBy"].AsValue().GetValue<bool>());
        }

    }
}
