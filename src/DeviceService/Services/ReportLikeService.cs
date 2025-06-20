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
                ("👏 Buddy on Your Report!", $"{user.Username} just gave your report \"{reportEntity.Name}\" a buddy!"),
                ("💪 Solid Work!", $"{user.Username} is loving your report \"{reportEntity.Name}\" – keep it up!"),
                ("🎉 Buddy Time!", $"{user.Username} just buddy’d your report \"{reportEntity.Name}\"."),
                ("🌟 Respect!", $"{user.Username} is showing some love for your report \"{reportEntity.Name}\"."),
                ("🔥 Got Your Back!", $"{user.Username} thinks your report \"{reportEntity.Name}\" is fire."),
                ("💺 Posture Matters!", $"{user.Username} buddy-approved \"{reportEntity.Name}\"."),
                ("🚀 You Got a Boost!", $"{user.Username} just gave props to your report \"{reportEntity.Name}\"."),
                ("🙌 BackBuddy!", $"{user.Username} is backing your report \"{reportEntity.Name}\" all the way."),
                ("✨ Buddy Power!", $"{user.Username} just hyped up \"{reportEntity.Name}\" – nice one!"),
                ("📈 Seen & Buddy’d!", $"{user.Username} noticed your progress in \"{reportEntity.Name}\" and gave it a thumbs up.")
            ];
            return messages[ThreadSafeRandom.Global.Next(messages.Count)];
        }
    }
}
