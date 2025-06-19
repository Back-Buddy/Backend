//using BackBuddy.Core.Library.Device.Dtos;

//namespace BackBuddy.Api.Test.V1
//{
//    [TestClass]
//    public class DeviceNotificationTest
//    {
//        [TestMethod]
//        public void SendNotification_ShouldReturnFalse_WhenDeviceIsInactive()
//        {
//            // Arrange
//            DeviceEntity device = new()
//            {
//                Id = Guid.NewGuid(),
//                Name = "TestDevice",
//                UserId = "user1",
//                SecretGeneratedAt = DateTime.UtcNow,
//                Active = false,
//                Threshold = TimeSpan.FromMinutes(30)
//            };

//            DeviceStatusDto status = new()
//            {
//                DeviceId = device.Id,
//                StartTime = DateTime.UtcNow.AddMinutes(-40)
//            };

//            // Act
//            bool result = Service.V1.Device.Services.DeviceService.SendNotification(device, status, null);

//            // Assert
//            Assert.IsFalse(result);
//        }

//        [TestMethod]
//        public void SendNotification_ShouldReturnFalse_WhenBelowThreshold()
//        {
//            // Arrange
//            DeviceEntity device = new()
//            {
//                Id = Guid.NewGuid(),
//                Name = "TestDevice",
//                UserId = "user1",
//                SecretGeneratedAt = DateTime.UtcNow,
//                Active = true,
//                Threshold = TimeSpan.FromMinutes(30)
//            };
//            DeviceStatusDto status = new()
//            {
//                DeviceId = device.Id,
//                StartTime = DateTime.UtcNow.AddMinutes(-10)
//            };

//            // Act
//            bool result = Service.V1.Device.Services.DeviceService.SendNotification(device, status, null);

//            // Assert
//            Assert.IsFalse(result);
//        }

//        [TestMethod]
//        public void SendNotification_ShouldReturnFalse_WhenLastNotificationIsRecent()
//        {
//            // Arrange
//            DeviceEntity device = new()
//            {
//                Id = Guid.NewGuid(),
//                Name = "TestDevice",
//                UserId = "user1",
//                SecretGeneratedAt = DateTime.UtcNow,
//                Active = true,
//                Threshold = TimeSpan.FromMinutes(30)
//            };
//            DeviceStatusDto status = new()
//            {
//                DeviceId = device.Id,
//                StartTime = DateTime.UtcNow.AddMinutes(-40)
//            };
//            DateTime lastNotification = DateTime.UtcNow.AddMinutes(-10);

//            // Act
//            bool result = Service.V1.Device.Services.DeviceService.SendNotification(device, status, lastNotification);

//            // Assert
//            Assert.IsFalse(result);
//        }

//        [TestMethod]
//        public void SendNotification_ShouldReturnTrue_WhenAllConditionsMet()
//        {
//            // Arrange
//            DeviceEntity device = new()
//            {
//                Id = Guid.NewGuid(),
//                Name = "TestDevice",
//                UserId = "user1",
//                SecretGeneratedAt = DateTime.UtcNow,
//                Active = true,
//                Threshold = TimeSpan.FromMinutes(30)
//            };
//            DeviceStatusDto status = new()
//            {
//                DeviceId = device.Id,
//                StartTime = DateTime.UtcNow.AddMinutes(-40)
//            };
//            DateTime lastNotification = DateTime.UtcNow.AddMinutes(-40);

//            // Act
//            bool result = Service.V1.Device.Services.DeviceService.SendNotification(device, status, lastNotification);

//            // Assert
//            Assert.IsTrue(result);
//        }

//        [TestMethod]
//        public void SendNotification_ShouldReturnTrue_WhenNoLastNotificationAndThresholdExceeded()
//        {
//            // Arrange
//            DeviceEntity device = new()
//            {
//                Id = Guid.NewGuid(),
//                Name = "TestDevice",
//                UserId = "user1",
//                SecretGeneratedAt = DateTime.UtcNow,
//                Active = true,
//                Threshold = TimeSpan.FromMinutes(30)
//            };
//            DeviceStatusDto status = new()
//            {
//                DeviceId = device.Id,
//                StartTime = DateTime.UtcNow.AddMinutes(-40)
//            };

//            // Act
//            bool result = Service.V1.Device.Services.DeviceService.SendNotification(device, status, null);

//            // Assert
//            Assert.IsTrue(result);
//        }
//    }
//}
