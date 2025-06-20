using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Core.Library.Device.Enums;
using BackBuddy.Core.Library.Device.Exceptions;
using BackBuddy.Core.Library.Notifications.Dtos;
using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.Device.Service.Entities;
using BackBuddy.Device.Service.Repositories;
using MassTransit;

namespace BackBuddy.Device.Service.Services
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
                    UserId = likerId
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

        private static (string Title, string Body) GetReportBuddyNotification(ReportEntity reportEntity, UserDto user)
        {
            List<(string Title, string Body)> messages =
            [
                ("👏 Buddy für deinen Report!", $"{user.Username} hat deinem Report \"{reportEntity.Name}\" einen Buddy gegeben."),
                ("💪 Starke Leistung!", $"{user.Username} feiert deinen Report \"{reportEntity.Name}\" – weiter so!"),
                ("🎉 Buddy-Time!", $"{user.Username} hat deinen Report \"{reportEntity.Name}\" gebuddyt."),
                ("🌟 Anerkennung für Haltung!", $"{user.Username} zeigt Respekt für deinen Report \"{reportEntity.Name}\"."),
                ("🔥 Rückenstark!", $"{user.Username} findet deinen Sitz-Report \"{reportEntity.Name}\" richtig gut."),
                ("💺 Haltung zählt!", $"{user.Username} gibt dir einen Buddy für \"{reportEntity.Name}\"."),
                ("🚀 Boost für dich!", $"{user.Username} hat deinen Report \"{reportEntity.Name}\" gewürdigt."),
                ("🙌 BackBuddy!", $"{user.Username} steht hinter deinem Report \"{reportEntity.Name}\"."),
                ("✨ Buddy-Power!", $"{user.Username} hat \"{reportEntity.Name}\" gefeiert – stark!"),
                ("📈 Gesehen & gebuddyt!", $"{user.Username} hat deinen Fortschritt in \"{reportEntity.Name}\" anerkannt.")
            ];
            return messages[ThreadSafeRandom.Global.Next(messages.Count)];
        }
    }
}
