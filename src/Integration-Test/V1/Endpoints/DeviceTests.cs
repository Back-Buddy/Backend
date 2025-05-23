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
            _deviceIds.Add(deviceId);

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

        [TestMethod]
        public async Task Test_ActivateDevice()
        {
            // Arrange
            string deviceName = "Chair 1";
            Guid deviceId = await _deviceLib.CreateSimpleDevice(_accessToken, deviceName);
            _deviceIds.Add(deviceId);

            // Act
            await _deviceLib.UpdateDevice(_accessToken, deviceId, active: true);

            // Assert
            JsonObject deviceObj = await _deviceLib.GetDevice(_accessToken, deviceId);
            Assert.IsNotNull(deviceObj);
            Assert.IsTrue(deviceObj["active"].GetValue<bool>());
        }

        [TestMethod]
        public async Task Test_ActivateDevice_Conflict()
        {
            // Arrange - Create and activate first device
            string deviceName1 = "Chair 1";
            Guid deviceId1 = await _deviceLib.CreateSimpleDevice(_accessToken, deviceName1);
            _deviceIds.Add(deviceId1);
            await _deviceLib.UpdateDevice(_accessToken, deviceId1, active: true);

            // Verify first device is active
            JsonObject device1 = await _deviceLib.GetDevice(_accessToken, deviceId1);
            Assert.IsTrue(device1["active"].GetValue<bool>(), "First device should be active");

            // Create second device
            string deviceName2 = "Chair 2";
            Guid deviceId2 = await _deviceLib.CreateSimpleDevice(_accessToken, deviceName2);
            _deviceIds.Add(deviceId2);

            // Act - Activate the second device (should succeed)
            await _deviceLib.UpdateDevice(_accessToken, deviceId2, active: true);

            // Assert - First device should now be inactive, second device should be active
            JsonObject device1AfterUpdate = await _deviceLib.GetDevice(_accessToken, deviceId1);
            Assert.IsFalse(device1AfterUpdate["active"].GetValue<bool>(), "First device should be automatically deactivated");

            JsonObject device2 = await _deviceLib.GetDevice(_accessToken, deviceId2);
            Assert.IsTrue(device2["active"].GetValue<bool>(), "Second device should be active");
        }

        [TestMethod]
        public async Task Test_GetDevices_FilteredByActive()
        {
            // Arrange
            List<Guid> activeDeviceIds = new();
            List<Guid> inactiveDeviceIds = new();
            
            // Create active devices
            for (int i = 0; i < 3; i++)
            {
                Guid deviceId = await _deviceLib.CreateSimpleDevice(_accessToken, $"Active Chair {i + 1}");
                await _deviceLib.UpdateDevice(_accessToken, deviceId, active: true);
                activeDeviceIds.Add(deviceId);
                _deviceIds.Add(deviceId);
            }
            
            // Create inactive devices
            for (int i = 0; i < 2; i++)
            {
                Guid deviceId = await _deviceLib.CreateSimpleDevice(_accessToken, $"Inactive Chair {i + 1}");
                inactiveDeviceIds.Add(deviceId);
                _deviceIds.Add(deviceId);
            }

            // Act & Assert - Get active devices
            (JsonArray activeDevices, _) = await _deviceLib.GetDevices(_accessToken, page: 0, active: true);
            Assert.AreEqual(3, activeDevices.Count);
            foreach (var device in activeDevices)
            {
                Assert.IsTrue(device.AsObject()["active"].GetValue<bool>(), "All devices should be active");
            }
            
            // Act & Assert - Get inactive devices
            (JsonArray inactiveDevices, _) = await _deviceLib.GetDevices(_accessToken, page: 0, active: false);
            Assert.AreEqual(2, inactiveDevices.Count);
            foreach (var device in inactiveDevices)
            {
                Assert.IsFalse(device.AsObject()["active"].GetValue<bool>(), "All devices should be inactive");
            }
            
            // Act & Assert - Get all devices
            (JsonArray allDevices, _) = await _deviceLib.GetDevices(_accessToken, page: 0);
            Assert.AreEqual(5, allDevices.Count, "Should get all devices when no active filter is specified");
        }
    }
}
