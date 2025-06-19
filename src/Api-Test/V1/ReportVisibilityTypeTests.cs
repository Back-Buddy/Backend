//using BackBuddy.Core.Library.Users.Dtos.Messages;
//using MassTransit;
//using Microsoft.Extensions.Logging;
//using NSubstitute;

//namespace BackBuddy.Api.Test.V1
//{
//    [TestClass]
//    public class ReportVisibilityTypeTests
//    {
//        [TestMethod]
//        public async Task GetReportVisibilityTypeForUser_ReturnsAllTypes_ForCreator()
//        {
//            // Arrange: Setup mocks and service
//            IUserRelationService relationService = Substitute.For<IUserRelationService>();
//            IDeviceLogRepository deviceLogRepository = Substitute.For<IDeviceLogRepository>();
//            IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
//            IReportRepository reportRepository = Substitute.For<IReportRepository>();
//            IReportLikeService reportLikeService = Substitute.For<IReportLikeService>();
//            IRequestClient<GetUserRequestMessage> requestClient = Substitute.For<IRequestClient<GetUserRequestMessage>>();
//            ILogger<ReportService> logger = Substitute.For<ILogger<ReportService>>();

//            ReportService reportService = new(reportLikeService, relationService, deviceLogRepository, deviceRepository, reportRepository, requestClient, logger);

//            string creatorId = "user1";
//            string userId = "user1";

//            // Act: Call the method under test
//            IEnumerable<ReportVisibilityType> result = await reportService.GetReportVisibilityTypeForUser(creatorId, userId);

//            // Assert: Check that all types are returned for the creator
//            List<ReportVisibilityType> expected =
//            [
//                ReportVisibilityType.All,
//                ReportVisibilityType.Followers,
//                ReportVisibilityType.Private
//            ];
//            CollectionAssert.AreEquivalent(expected, new List<ReportVisibilityType>(result));
//        }

//        [TestMethod]
//        public async Task GetReportVisibilityTypeForUser_ReturnsAllAndFollowers_ForStrongRelation()
//        {
//            // Arrange: Setup mocks and service
//            IUserRelationService relationService = Substitute.For<IUserRelationService>();
//            IDeviceLogRepository deviceLogRepository = Substitute.For<IDeviceLogRepository>();
//            IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
//            IReportRepository reportRepository = Substitute.For<IReportRepository>();
//            IRequestClient<GetUserRequestMessage> requestClient = Substitute.For<IRequestClient<GetUserRequestMessage>>();
//            ILogger<ReportService> logger = Substitute.For<ILogger<ReportService>>();

//            relationService.HasStrongRelation("user2", "user1").Returns(Task.FromResult(true));
//            IReportLikeService reportLikeService = Substitute.For<IReportLikeService>();
//            ReportService reportService = new(reportLikeService, relationService, deviceLogRepository, deviceRepository, reportRepository, requestClient, logger);

//            string creatorId = "user1";
//            string userId = "user2";

//            // Act: Call the method under test
//            IEnumerable<ReportVisibilityType> result = await reportService.GetReportVisibilityTypeForUser(creatorId, userId);

//            // Assert: Check that All and Followers are returned for strong relation
//            List<ReportVisibilityType> expected =
//            [
//                ReportVisibilityType.All,
//                ReportVisibilityType.Followers
//            ];
//            CollectionAssert.AreEquivalent(expected, new List<ReportVisibilityType>(result));
//        }

//        [TestMethod]
//        public async Task GetReportVisibilityTypeForUser_ReturnsAll_ForNoRelation()
//        {
//            // Arrange: Setup mocks and service
//            IUserRelationService relationService = Substitute.For<IUserRelationService>();
//            IDeviceLogRepository deviceLogRepository = Substitute.For<IDeviceLogRepository>();
//            IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
//            IReportRepository reportRepository = Substitute.For<IReportRepository>();
//            IReportLikeService reportLikeService = Substitute.For<IReportLikeService>();
//            IRequestClient<GetUserRequestMessage> requestClient = Substitute.For<IRequestClient<GetUserRequestMessage>>();
//            ILogger<ReportService> logger = Substitute.For<ILogger<ReportService>>();

//            relationService.HasStrongRelation("user3", "user1").Returns(Task.FromResult(false));
//            ReportService reportService = new(reportLikeService, relationService, deviceLogRepository, deviceRepository, reportRepository, requestClient, logger);

//            string creatorId = "user1";
//            string userId = "user3";

//            // Act: Call the method under test
//            IEnumerable<ReportVisibilityType> result = await reportService.GetReportVisibilityTypeForUser(creatorId, userId);

//            // Assert: Check that only All is returned for no relation
//            List<ReportVisibilityType> expected =
//            [
//                ReportVisibilityType.All
//            ];
//            CollectionAssert.AreEquivalent(expected, new List<ReportVisibilityType>(result));
//        }
//    }
//}