using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Enums;
using BackBuddy.Api.Service.V1.Device.Exceptions;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Notifications.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos.Messages;
using BackBuddy.Api.Service.V1.Utilities;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Device.Services
{
    public interface IReportLikeService
    {
        Task AddLike(string userId, ReportEntity report, IEnumerable<ReportVisibilityType> visibilityTypes, CancellationToken cancellationToken = default);
        Task DeleteAllLikesFromUser(string userId, CancellationToken cancellationToken = default);
        Task DeleteAllLikesFromReport(Guid reportId, CancellationToken cancellationToken = default);
        Task DeleteLike(string userId, Guid reportId, CancellationToken cancellationToken = default);
        Task<bool> HasLikedReport(string userId, Guid reportId, CancellationToken cancellationToken = default);
        Task<Page<List<Guid>>> GetReportLikesFromUser(string userid, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<Page<List<string>>> GetReportLikesFromReport(ReportEntity report, IEnumerable<ReportVisibilityType> visibilityTypes, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<long> CountLikesFromUser(string userId, CancellationToken cancellationToken = default);
        Task<long> CountLikesFromReport(Guid reportId, CancellationToken cancellationToken = default);
    }
    public class ReportLikeService(IReportLikeRepository repository, IRequestClient<GetUserRequestMessage> userRequestClient, IRequestClient<GetFcmTokensRequestMessage> fcmRequestClient, IPublishEndpoint publishEndpoint, ILogger<ReportLikeService> logger) : IReportLikeService
    {
        private readonly IReportLikeRepository _repository = repository;
        private readonly IRequestClient<GetUserRequestMessage> _userRequestClient = userRequestClient;
        private readonly IRequestClient<GetFcmTokensRequestMessage> _fcmRequestClient = fcmRequestClient;
        private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
        private readonly ILogger<ReportLikeService> _logger = logger;

        public async Task AddLike(string userId, ReportEntity report, IEnumerable<ReportVisibilityType> visibilityTypes, CancellationToken cancellationToken = default)
        {
            if (!visibilityTypes.Contains(report.VisibilityType))
                throw new ReportNotFoundException();
            if (userId == report.UserId)
                throw new ReportLikeCannotLikeOwnReportException();

            bool alreadyLiked = await HasLikedReport(userId, report.Id, cancellationToken);
            if (alreadyLiked)
                throw new ReportLikeAlreadyLikedException();

            ReportLikeEntity reportLikeEntity = new()
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ReportId = report.Id
            };

            await _repository.AddLike(reportLikeEntity, cancellationToken);

            await NotifyReportLike(report, userId);
        }

        private async Task NotifyReportLike(ReportEntity reportEntity, string likerId)
        {
            try
            {
                Response<GetUserResponseMessage> userResponse = await _userRequestClient.GetResponse<GetUserResponseMessage>(new GetUserRequestMessage { UserId = reportEntity.UserId });

                GetFcmTokensRequestMessage request = new()
                {
                    UserId = reportEntity.UserId
                };
                Response<GetFcmTokensResponseMessage> response = await _fcmRequestClient.GetResponse<GetFcmTokensResponseMessage>(request);

                UserDto user = userResponse.Message.User;
                IEnumerable<string> tokens = response.Message.Tokens;

                (string title, string body) = GetReportBuddyNotification(reportEntity, user);
                await _publishEndpoint.Publish(new SendNotificationRequestMessage
                {
                    Notification = new NotificationBuilder().SetTitle(title).SetBody(body).Build(),
                    Tokens = tokens
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to notify report like for report {ReportId} by user {LikerId}", reportEntity.Id, likerId);
            }
        }

        public async Task<long> CountLikesFromReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            return await _repository.CountLikesFromReport(reportId, cancellationToken);
        }

        public async Task<long> CountLikesFromUser(string userId, CancellationToken cancellationToken = default)
        {
            return await _repository.CountLikesFromUser(userId, cancellationToken);
        }

        public async Task DeleteAllLikesFromReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAllLikesFromReport(reportId, cancellationToken);
        }

        public async Task DeleteAllLikesFromUser(string userId, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAllLikesFromUser(userId, cancellationToken);
        }

        public async Task DeleteLike(string userId, Guid reportId, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteLike(userId, reportId, cancellationToken);
        }

        public async Task<Page<List<string>>> GetReportLikesFromReport(ReportEntity report, IEnumerable<ReportVisibilityType> visibilityTypes, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            if (!visibilityTypes.Contains(report.VisibilityType))
                throw new ReportNotFoundException();
            Page<List<ReportLikeEntity>> result = await _repository.GetReportLikesFromReport(report.Id, page, cancellationToken);
            return new Page<List<string>>
            {
                Items = [.. result.Items.Select(x => x.UserId)],
                HasMoreEntries = result.HasMoreEntries
            };
        }

        public async Task<Page<List<Guid>>> GetReportLikesFromUser(string userid, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            Page<List<ReportLikeEntity>> result = await _repository.GetReportLikesFromUser(userid, page, cancellationToken);
            return new Page<List<Guid>>
            {
                Items = [.. result.Items.Select(x => x.ReportId)],
                HasMoreEntries = result.HasMoreEntries
            };
        }

        public async Task<bool> HasLikedReport(string userId, Guid reportId, CancellationToken cancellationToken = default)
        {
            return await _repository.HasLikedReport(userId, reportId, cancellationToken);
        }

        private static readonly List<(string Title, string Body)> _reportBuddyMessages =
        [
            ("👏 Buddy für deinen Report!", "{0} hat deinem Report \"{1}\" einen Buddy gegeben."),
            ("💪 Starke Leistung!", "{0} feiert deinen Report \"{1}\" – weiter so!"),
            ("🎉 Buddy-Time!", "{0} hat deinen Report \"{1}\" gebuddyt."),
            ("🌟 Anerkennung für Haltung!", "{0} zeigt Respekt für deinen Report \"{1}\"."),
            ("🔥 Rückenstark!", "{0} findet deinen Sitz-Report \"{1}\" richtig gut."),
            ("💺 Haltung zählt!", "{0} gibt dir einen Buddy für \"{1}\"."),
            ("🚀 Boost für dich!", "{0} hat deinen Report \"{1}\" gewürdigt."),
            ("🙌 BackBuddy!", "{0} steht hinter deinem Report \"{1}\"."),
            ("✨ Buddy-Power!", "{0} hat \"{1}\" gefeiert – stark!"),
            ("📈 Gesehen & gebuddyt!", "{0} hat deinen Fortschritt in \"{1}\" anerkannt.")
        ];

        private static (string Title, string Body) GetReportBuddyNotification(ReportEntity reportEntity, UserDto user)
        {
            (string body, string title) = _reportBuddyMessages[ThreadSafeRandom.Global.Next(_reportBuddyMessages.Count)];
            return (title, string.Format(body, user.Username, reportEntity.Name));
        }
    }
}
