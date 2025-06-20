using BackBuddy.Core.Library.Device.Enums;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Device.Service.Repositories;
using BackBuddy.Device.Service.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BackBuddy.Device.Test.V1
{
    [TestClass]
    public class ReportVisibilityTypeTests
    {
        [TestMethod]
        public async Task GetReportVisibilityTypeForUser_ReturnsAllTypes_ForCreator()
        {
            // Arrange: Setup mocks and service
            IDeviceLogRepository deviceLogRepository = Substitute.For<IDeviceLogRepository>();
            IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
            IReportRepository reportRepository = Substitute.For<IReportRepository>();
            IReportLikeService reportLikeService = Substitute.For<IReportLikeService>();
            IRequestClient<GetUserRequestMessage> requestClient = Substitute.For<IRequestClient<GetUserRequestMessage>>();
            IRequestClient<GetStrongFollowRelationsAndAllFollowingsRequestMessage> getStrongFollowRelationsAndAllFollowingsClient = Substitute.For<IRequestClient<GetStrongFollowRelationsAndAllFollowingsRequestMessage>>();
            IRequestClient<HasUserStrongRelationRequestMessage> hasUserStrongRelationClient = Substitute.For<IRequestClient<HasUserStrongRelationRequestMessage>>();
            ILogger<ReportService> logger = Substitute.For<ILogger<ReportService>>();

            ReportService reportService = new(reportLikeService, deviceLogRepository, deviceRepository, reportRepository, requestClient, getStrongFollowRelationsAndAllFollowingsClient, hasUserStrongRelationClient, logger);

            string creatorIdTest = "user1";
            string userIdTest = "user1";

            // Act: Call the method under test
            IEnumerable<ReportVisibilityType> result = await reportService.GetReportVisibilityTypeForUser(userIdTest, creatorIdTest);

            // Assert: Check that all types are returned for the creator
            List<ReportVisibilityType> expected =
            [
                ReportVisibilityType.All,
                ReportVisibilityType.Followers,
                ReportVisibilityType.Private
            ];
            CollectionAssert.AreEquivalent(expected, result.ToList());
        }

        [TestMethod]
        public async Task GetReportVisibilityTypeForUser_ReturnsAllAndFollowers_ForStrongRelation()
        {
            // Arrange: Setup mocks and service
            IDeviceLogRepository deviceLogRepository = Substitute.For<IDeviceLogRepository>();
            IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
            IReportRepository reportRepository = Substitute.For<IReportRepository>();
            IReportLikeService reportLikeService = Substitute.For<IReportLikeService>();
            IRequestClient<GetUserRequestMessage> requestClient = Substitute.For<IRequestClient<GetUserRequestMessage>>();
            IRequestClient<GetStrongFollowRelationsAndAllFollowingsRequestMessage> getStrongFollowRelationsAndAllFollowingsClient = Substitute.For<IRequestClient<GetStrongFollowRelationsAndAllFollowingsRequestMessage>>();
            IRequestClient<HasUserStrongRelationRequestMessage> hasUserStrongRelationClient = Substitute.For<IRequestClient<HasUserStrongRelationRequestMessage>>();
            ILogger<ReportService> logger = Substitute.For<ILogger<ReportService>>();

            var response = Substitute.For<Response<HasUserStrongRelationResponseMessage>>();
            var responseMessage = new HasUserStrongRelationResponseMessage { HasStrongRelation = true };
            response.Message.Returns(responseMessage);

            hasUserStrongRelationClient.GetResponse<HasUserStrongRelationResponseMessage>(Arg.Any<HasUserStrongRelationRequestMessage>()).Returns(Task.FromResult(response));

            ReportService reportService = new(reportLikeService, deviceLogRepository, deviceRepository, reportRepository, requestClient, getStrongFollowRelationsAndAllFollowingsClient, hasUserStrongRelationClient, logger);

            string creatorIdTest = "user1";
            string userIdTest = "user2";

            // Act: Call the method under test
            IEnumerable<ReportVisibilityType> result = await reportService.GetReportVisibilityTypeForUser(userIdTest, creatorIdTest);

            // Assert: Check that All and Followers are returned for strong relation
            List<ReportVisibilityType> expected =
            [
                ReportVisibilityType.All,
                ReportVisibilityType.Followers
            ];
            CollectionAssert.AreEquivalent(expected, result.ToList());
        }

        [TestMethod]
        public async Task GetReportVisibilityTypeForUser_ReturnsAll_ForNoRelation()
        {
            // Arrange: Setup mocks and service
            IDeviceLogRepository deviceLogRepository = Substitute.For<IDeviceLogRepository>();
            IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
            IReportRepository reportRepository = Substitute.For<IReportRepository>();
            IReportLikeService reportLikeService = Substitute.For<IReportLikeService>();
            IRequestClient<GetUserRequestMessage> requestClient = Substitute.For<IRequestClient<GetUserRequestMessage>>();
            IRequestClient<GetStrongFollowRelationsAndAllFollowingsRequestMessage> getStrongFollowRelationsAndAllFollowingsClient = Substitute.For<IRequestClient<GetStrongFollowRelationsAndAllFollowingsRequestMessage>>();
            IRequestClient<HasUserStrongRelationRequestMessage> hasUserStrongRelationClient = Substitute.For<IRequestClient<HasUserStrongRelationRequestMessage>>();
            ILogger<ReportService> logger = Substitute.For<ILogger<ReportService>>();

            var response = Substitute.For<Response<HasUserStrongRelationResponseMessage>>();
            var responseMessage = new HasUserStrongRelationResponseMessage { HasStrongRelation = false };
            response.Message.Returns(responseMessage);

            hasUserStrongRelationClient.GetResponse<HasUserStrongRelationResponseMessage>(Arg.Any<HasUserStrongRelationRequestMessage>()).Returns(Task.FromResult(response));


            ReportService reportService = new(reportLikeService, deviceLogRepository, deviceRepository, reportRepository, requestClient, getStrongFollowRelationsAndAllFollowingsClient, hasUserStrongRelationClient, logger);

            string creatorIdTest = "user1";
            string userIdTest = "user3";

            // Act: Call the method under test
            IEnumerable<ReportVisibilityType> result = await reportService.GetReportVisibilityTypeForUser(userIdTest, creatorIdTest);

            // Assert: Check that only All is returned for no relation
            List<ReportVisibilityType> expected =
            [
                ReportVisibilityType.All
            ];
            CollectionAssert.AreEquivalent(expected, result.ToList());
        }
    }
}