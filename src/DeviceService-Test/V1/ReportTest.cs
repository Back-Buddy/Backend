using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Core.Library.Device.Exceptions;
using BackBuddy.Device.Service.Entities;
using BackBuddy.Device.Service.Services;

namespace BackBuddy.Device.Test.V1
{
    [TestClass]
    public class ReportTest
    {
        [TestMethod]
        public void AnalyzeLogs_WithMultipleSitLogs_CalculatesAllFieldsCorrectly()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 18, 0, 0, DateTimeKind.Utc);
            TimeSpan totalTime = endTime - startTime; // 10 hours

            DeviceLogEntity log1 = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            DeviceLogEntity log2 = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 14, 0, 0, DateTimeKind.Utc),
                EndTime = new(2023, 1, 1, 16, 0, 0, DateTimeKind.Utc)
            };
            List<DeviceLogEntity> logs = [log1, log2];

            // 2 hours + 2 hours = 4 hours sitting
            TimeSpan expectedSitTime = TimeSpan.FromHours(4);
            // 10 hours - 4 hours = 6 hours standing
            TimeSpan expectedStandTime = totalTime - expectedSitTime;
            double expectedSitPercentage = 40.0; // (4 / 10) * 100
            double expectedStandPercentage = 60.0; // (6 / 10) * 100

            // Act
            (ReportMetadataEntity result, IEnumerable<DeviceLogEntity> usedLogs) = ReportService.AnalyzeLogs(logs, startTime, endTime);

            // Assert
            Assert.AreEqual(totalTime, result.TotalTime);
            Assert.AreEqual(expectedSitTime, result.SitTime);
            Assert.AreEqual(expectedStandTime, result.StandTime);
            Assert.AreEqual(expectedSitPercentage, result.SitPercentage);
            Assert.AreEqual(expectedStandPercentage, result.StandPercentage);
            Assert.AreEqual(2, result.PostureChanges);
            Assert.AreEqual(TimeSpan.FromHours(2), result.LongestSitPeriod);
            Assert.AreEqual(TimeSpan.FromHours(2), result.ShortestSitPeriod);
            Assert.AreEqual(TimeSpan.FromHours(2), result.AverageSitPeriod);
            Assert.AreEqual(2, usedLogs.Count());
            Assert.IsTrue(usedLogs.Contains(log1));
            Assert.IsTrue(usedLogs.Contains(log2));
        }

        [TestMethod]
        public void AnalyzeLogs_WithNoLogs_AllSitFieldsZeroOrNull()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 18, 0, 0, DateTimeKind.Utc);
            TimeSpan totalTime = endTime - startTime; // 10 hours

            List<DeviceLogEntity> logs = [];

            // Act
            (ReportMetadataEntity result, IEnumerable<DeviceLogEntity> usedLogs) = ReportService.AnalyzeLogs(logs, startTime, endTime);

            // Assert
            Assert.AreEqual(totalTime, result.TotalTime);
            Assert.AreEqual(TimeSpan.Zero, result.SitTime);
            Assert.AreEqual(totalTime, result.StandTime);
            Assert.AreEqual(0.0, result.SitPercentage);
            Assert.AreEqual(100.0, result.StandPercentage);
            Assert.AreEqual(0, result.PostureChanges);
            Assert.IsNull(result.LongestSitPeriod);
            Assert.IsNull(result.ShortestSitPeriod);
            Assert.IsNull(result.AverageSitPeriod);
            Assert.AreEqual(0, usedLogs.Count());
        }

        [TestMethod]
        public void AnalyzeLogs_WithNonSitLogs_AllSitFieldsZeroOrNull()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 18, 0, 0, DateTimeKind.Utc);
            TimeSpan totalTime = endTime - startTime; // 10 hours

            DeviceLogEntity log = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Error,
                StartTime = new(2023, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            List<DeviceLogEntity> logs = [log];

            // Act
            (ReportMetadataEntity result, IEnumerable<DeviceLogEntity> usedLogs) = ReportService.AnalyzeLogs(logs, startTime, endTime);

            // Assert
            Assert.AreEqual(totalTime, result.TotalTime);
            Assert.AreEqual(TimeSpan.Zero, result.SitTime);
            Assert.AreEqual(totalTime, result.StandTime);
            Assert.AreEqual(0.0, result.SitPercentage);
            Assert.AreEqual(100.0, result.StandPercentage);
            Assert.AreEqual(0, result.PostureChanges);
            Assert.IsNull(result.LongestSitPeriod);
            Assert.IsNull(result.ShortestSitPeriod);
            Assert.IsNull(result.AverageSitPeriod);
            Assert.AreEqual(0, usedLogs.Count());
        }

        [TestMethod]
        public void AnalyzeLogs_WithZeroDuration_ThrowsException()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc); // Same as start time

            DeviceLogEntity log = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            List<DeviceLogEntity> logs = [log];

            // Act && Assert
            Assert.ThrowsExactly<ReportInvalidTimeFilterException>(() => ReportService.AnalyzeLogs(logs, startTime, endTime));
        }

        [TestMethod]
        public void AnalyzeLogs_WithNegativeDuration_ThrowsException()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc); // End before start

            List<DeviceLogEntity> logs = [];

            // Act && Assert
            Assert.ThrowsExactly<ReportInvalidTimeFilterException>(() => ReportService.AnalyzeLogs(logs, startTime, endTime));
        }

        [TestMethod]
        public void AnalyzeLogs_WithOneSitLog_AllPeriodsEqual()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 18, 0, 0, DateTimeKind.Utc);

            DeviceLogEntity log = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            List<DeviceLogEntity> logs = [log];

            // Act
            (ReportMetadataEntity result, IEnumerable<DeviceLogEntity> usedLogs) = ReportService.AnalyzeLogs(logs, startTime, endTime);

            // Assert
            Assert.AreEqual(1, result.PostureChanges);
            Assert.AreEqual(TimeSpan.FromHours(2), result.LongestSitPeriod);
            Assert.AreEqual(TimeSpan.FromHours(2), result.ShortestSitPeriod);
            Assert.AreEqual(TimeSpan.FromHours(2), result.AverageSitPeriod);
            Assert.AreEqual(1, usedLogs.Count());
            Assert.AreEqual(log, usedLogs.First());
        }

        [TestMethod]
        public void AnalyzeLogs_WithLogsOutsideTimeRange_IgnoresThem()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 18, 0, 0, DateTimeKind.Utc);
            TimeSpan totalTime = endTime - startTime; // 10 hours

            DeviceLogEntity log1 = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 6, 0, 0, DateTimeKind.Utc),  // Before startTime
                EndTime = new(2023, 1, 1, 7, 0, 0, DateTimeKind.Utc)     // Before startTime
            };
            DeviceLogEntity log2 = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 19, 0, 0, DateTimeKind.Utc), // After endTime
                EndTime = new(2023, 1, 1, 20, 0, 0, DateTimeKind.Utc)    // After endTime
            };
            List<DeviceLogEntity> logs = [log1, log2];

            // Act
            (ReportMetadataEntity result, IEnumerable<DeviceLogEntity> usedLogs) = ReportService.AnalyzeLogs(logs, startTime, endTime);

            // Assert
            Assert.AreEqual(totalTime, result.TotalTime);
            Assert.AreEqual(TimeSpan.Zero, result.SitTime);
            Assert.AreEqual(totalTime, result.StandTime);
            Assert.AreEqual(0.0, result.SitPercentage);
            Assert.AreEqual(100.0, result.StandPercentage);
            Assert.AreEqual(0, result.PostureChanges);
            Assert.IsNull(result.LongestSitPeriod);
            Assert.IsNull(result.ShortestSitPeriod);
            Assert.IsNull(result.AverageSitPeriod);
            Assert.AreEqual(0, usedLogs.Count());
        }

        [TestMethod]
        public void AnalyzeLogs_WithLogsExactlyOnBoundaries_IncludesThem()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 18, 0, 0, DateTimeKind.Utc);
            TimeSpan totalTime = endTime - startTime; // 10 hours

            DeviceLogEntity log1 = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = startTime,
                EndTime = new(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            };
            DeviceLogEntity log2 = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 16, 0, 0, DateTimeKind.Utc),
                EndTime = endTime
            };
            List<DeviceLogEntity> logs = [log1, log2];

            // 2 hours + 2 hours = 4 hours sitting
            TimeSpan expectedSitTime = TimeSpan.FromHours(4);
            TimeSpan expectedStandTime = totalTime - expectedSitTime;

            // Act
            (ReportMetadataEntity result, IEnumerable<DeviceLogEntity> usedLogs) = ReportService.AnalyzeLogs(logs, startTime, endTime);

            // Assert
            Assert.AreEqual(2, result.PostureChanges);
            Assert.AreEqual(TimeSpan.FromHours(2), result.LongestSitPeriod);
            Assert.AreEqual(TimeSpan.FromHours(2), result.ShortestSitPeriod);
            Assert.AreEqual(TimeSpan.FromHours(2), result.AverageSitPeriod);
            Assert.AreEqual(expectedSitTime, result.SitTime);
            Assert.AreEqual(expectedStandTime, result.StandTime);
            Assert.AreEqual(2, usedLogs.Count());
            Assert.IsTrue(usedLogs.Contains(log1));
            Assert.IsTrue(usedLogs.Contains(log2));
        }

        [TestMethod]
        public void AnalyzeLogs_WithLogsPartiallyOverlappingTimeRange_ExcludesThem()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 18, 0, 0, DateTimeKind.Utc);
            TimeSpan totalTime = endTime - startTime; // 10 hours

            DeviceLogEntity log1 = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 7, 0, 0, DateTimeKind.Utc),  // 1 hour before startTime
                EndTime = new(2023, 1, 1, 9, 0, 0, DateTimeKind.Utc)     // 1 hour after startTime
            };
            DeviceLogEntity log2 = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 17, 0, 0, DateTimeKind.Utc), // 1 hour before endTime
                EndTime = new(2023, 1, 1, 19, 0, 0, DateTimeKind.Utc)    // 1 hour after endTime
            };
            List<DeviceLogEntity> logs = [log1, log2];

            // Act
            (ReportMetadataEntity result, IEnumerable<DeviceLogEntity> usedLogs) = ReportService.AnalyzeLogs(logs, startTime, endTime);

            // Assert
            Assert.AreEqual(0, result.PostureChanges);
            Assert.IsNull(result.LongestSitPeriod);
            Assert.IsNull(result.ShortestSitPeriod);
            Assert.IsNull(result.AverageSitPeriod);
            Assert.AreEqual(TimeSpan.Zero, result.SitTime);
            Assert.AreEqual(totalTime, result.StandTime);
            Assert.AreEqual(0.0, result.SitPercentage);
            Assert.AreEqual(100.0, result.StandPercentage);
            Assert.AreEqual(0, usedLogs.Count());
        }

        [TestMethod]
        public void AnalyzeLogs_WithMixedLogTypes_OnlyCountsSitLogs()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 18, 0, 0, DateTimeKind.Utc);

            DeviceLogEntity sitLog = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            };
            DeviceLogEntity errorLog = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Error,
                StartTime = new(2023, 1, 1, 14, 0, 0, DateTimeKind.Utc),
                EndTime = new(2023, 1, 1, 16, 0, 0, DateTimeKind.Utc)
            };
            List<DeviceLogEntity> logs = [sitLog, errorLog];

            // Act
            (ReportMetadataEntity result, IEnumerable<DeviceLogEntity> usedLogs) = ReportService.AnalyzeLogs(logs, startTime, endTime);

            // Assert
            Assert.AreEqual(1, result.PostureChanges);
            Assert.AreEqual(TimeSpan.FromHours(2), result.LongestSitPeriod);
            Assert.AreEqual(TimeSpan.FromHours(2), result.ShortestSitPeriod);
            Assert.AreEqual(TimeSpan.FromHours(2), result.AverageSitPeriod);
            Assert.AreEqual(TimeSpan.FromHours(2), result.SitTime);
            Assert.AreEqual(TimeSpan.FromHours(8), result.StandTime);
            Assert.AreEqual(20.0, result.SitPercentage);
            Assert.AreEqual(80.0, result.StandPercentage);
            Assert.AreEqual(1, usedLogs.Count());
            Assert.AreEqual(sitLog, usedLogs.First());
        }

        [TestMethod]
        public void AnalyzeLogs_WithSitLogOfZeroDuration_HandlesCorrectly()
        {
            // Arrange
            DateTime startTime = new(2023, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new(2023, 1, 1, 18, 0, 0, DateTimeKind.Utc);

            DeviceLogEntity log = new()
            {
                Id = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                LogType = Core.Library.Device.Enums.DeviceLogType.Sit,
                StartTime = new(2023, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new(2023, 1, 1, 9, 0, 0, DateTimeKind.Utc)
            };
            List<DeviceLogEntity> logs = [log];

            // Act
            (ReportMetadataEntity result, IEnumerable<DeviceLogEntity> usedLogs) = ReportService.AnalyzeLogs(logs, startTime, endTime);

            // Assert
            Assert.AreEqual(1, result.PostureChanges);
            Assert.AreEqual(TimeSpan.Zero, result.LongestSitPeriod);
            Assert.AreEqual(TimeSpan.Zero, result.ShortestSitPeriod);
            Assert.AreEqual(TimeSpan.Zero, result.AverageSitPeriod);
            Assert.AreEqual(TimeSpan.Zero, result.SitTime);
            Assert.AreEqual(TimeSpan.FromHours(10), result.StandTime);
            Assert.AreEqual(0.0, result.SitPercentage);
            Assert.AreEqual(100.0, result.StandPercentage);
            Assert.AreEqual(1, usedLogs.Count());
            Assert.AreEqual(log, usedLogs.First());
        }
    }
}
