using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.V1.DTOs;
using BackBuddy.Integration_Test.V1.Libs;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Endpoints
{
    [TestClass]
    public class DeviceTests
    {
        private static DeviceLib _deviceLib;
        private static string _accessToken;
        private static string _userId;
        private static FirebaseLib _firebaseLib;

        private readonly static List<Guid> _deviceIds = [];

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _accessToken = Environment.GetEnvironmentVariable("E2E_ACCESS_TOKEN");
            _userId = Environment.GetEnvironmentVariable("E2E_USER_ID");
            Uri baseUri = new(Environment.GetEnvironmentVariable("E2E_BASE_URI") ?? "http://localhost:8080/");

            _deviceLib = new DeviceLib(baseUri.ToString());

            if(_accessToken == null)
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
            foreach(Guid deviceId in _deviceIds)
            {
                await _deviceLib.DeleteDevice(_accessToken, deviceId);
            }
            _deviceIds.Clear();
        }

        [TestMethod]
        public async Task Test_CreateDevice()
        {
            // Arrange
            string deviceName = "Chair 1";

            // Act
            JsonObject secretObj = await _deviceLib.CreateDevice(_accessToken, deviceName);

            // Assert
            Assert.IsNotNull(secretObj);

            Guid deviceId = Guid.Parse(secretObj["deviceId"].AsValue().GetValue<string>());
            _deviceIds.Add(deviceId);
            Assert.IsNotNull(secretObj["secret"].AsValue().GetValue<string>());
        }

        [TestMethod]
        public async Task Test_CreateDevice_Duplicated_Name()
        {
            // Arrange
            string deviceName = "Chair 1";
            Guid deviceId = await _deviceLib.CreateSimpleDevice(_accessToken, deviceName);

            // Act & Assert
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _deviceLib.CreateDevice(_accessToken, deviceName.ToLower()));
            Assert.AreEqual(System.Net.HttpStatusCode.Conflict, requestFailedException.ResponseMessage.StatusCode);
            
            string rawContent = await requestFailedException.ResponseMessage.Content.ReadAsStringAsync();
            JsonArray errorInformation = JsonSerializer.Deserialize<JsonArray>(rawContent);
            Assert.AreEqual("Device.NameIsNotUnique", errorInformation[0]["Code"].GetValue<string>());
        }

        [TestMethod]
        [DataRow("Chair01")]
        [DataRow("Office-Chair")]
        [DataRow("Model X")]
        [DataRow("Seat-123")]
        [DataRow("Lounge12")]
        [DataRow("Gaming Chair")]
        [DataRow("Chair-Pro")]
        public async Task Test_CreateDevice_Valid_Name(string deviceName)
        {
            // Arrange & Act
            JsonObject secretObj = await _deviceLib.CreateDevice(_accessToken, deviceName);

            // Assert
            Assert.IsNotNull(secretObj);

            Guid deviceId = Guid.Parse(secretObj["deviceId"].AsValue().GetValue<string>());
            _deviceIds.Add(deviceId);
            Assert.IsNotNull(secretObj["secret"].AsValue().GetValue<string>());
        }

        [TestMethod]
        [DataRow("Ch")]               
        [DataRow("Chair_Name")]
        [DataRow("Deluxe*Chair")]   
        [DataRow("ExtraComfortEdition")]
        [DataRow("Chair@Home")]
        [DataRow("Chair!")]
        public async Task Test_CreateDevice_InValid_Name(string deviceName)
        {
            // Arrange & Act
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _deviceLib.CreateDevice(_accessToken, deviceName));

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, requestFailedException.ResponseMessage.StatusCode);

            string rawContent = await requestFailedException.ResponseMessage.Content.ReadAsStringAsync();
            JsonArray errorInformation = JsonSerializer.Deserialize<JsonArray>(rawContent);
            Assert.AreEqual("Device.InvalidName", errorInformation[0]["Code"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_UpdateDevice()
        {
            // Arrange
            string beforeDeviceName = "Chair 1";
            string afterDeviceName = "Chair 2";
            TimeSpan threshold = TimeSpan.FromHours(1);

            Guid deviceId = await _deviceLib.CreateSimpleDevice(_accessToken, beforeDeviceName);
            _deviceIds.Add(deviceId);

            // Act
            await _deviceLib.UpdateDevice(_accessToken, deviceId, afterDeviceName, TimeSpan.FromHours(1));

            // Assert
            JsonObject deviceObj = await _deviceLib.GetDevice(_accessToken, deviceId);
            Assert.IsNotNull(deviceObj);

            Assert.AreEqual(afterDeviceName, deviceObj["name"].GetValue<string>());
            Assert.AreEqual(threshold, TimeSpan.Parse(deviceObj["threshold"].GetValue<string>()));
        }

        [TestMethod]
        public async Task Test_DeleteDevice()
        {
            // Arrange
            Guid deviceId = await _deviceLib.CreateSimpleDevice(_accessToken, "Chair 1");

            // Act
            await _deviceLib.DeleteDevice(_accessToken, deviceId);

            // Assert
            RequestFailedException requestFailedException = await Assert.ThrowsExactlyAsync<RequestFailedException>(async () => await _deviceLib.GetDevice(_accessToken, deviceId));
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, requestFailedException.ResponseMessage.StatusCode);

            string rawContent = await requestFailedException.ResponseMessage.Content.ReadAsStringAsync();
            JsonArray errorInformation = JsonSerializer.Deserialize<JsonArray>(rawContent);
            Assert.AreEqual("Device.NotFound", errorInformation[0]["Code"].GetValue<string>());
        }

        [TestMethod]
        public async Task Test_GetDevies()
        {
            // Arrange
            for (int i = 0; i < 21; i++)
            {
                Guid deviceId = await _deviceLib.CreateSimpleDevice(_accessToken, $"Chair {i + 1}");
                _deviceIds.Add(deviceId);
            }

            List<string> deviceNames = [];

            // Act & Assert
            (JsonArray devices, bool hasMoreResults) = await _deviceLib.GetDevices(_accessToken, 1, 10);
            Assert.AreEqual(10, devices.Count);
            Assert.IsTrue(hasMoreResults);
            deviceNames.AddRange(devices.Select(d => d["name"].GetValue<string>()));

            (devices, hasMoreResults) = await _deviceLib.GetDevices(_accessToken, 2, 10);
            Assert.AreEqual(10, devices.Count);
            Assert.IsTrue(hasMoreResults);
            deviceNames.AddRange(devices.Select(d => d["name"].GetValue<string>()));

            (devices, hasMoreResults) = await _deviceLib.GetDevices(_accessToken, 3, 10);
            Assert.AreEqual(1, devices.Count);
            Assert.IsFalse(hasMoreResults);
            deviceNames.AddRange(devices.Select(d => d["name"].GetValue<string>()));

            for (int i = 0; i < 21; i++)
            {
                Assert.IsTrue(deviceNames.Contains($"Chair {i + 1}"), $"Device name not found: Chair {i + 1}");
            }
        }
    }
}
