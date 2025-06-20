using BackBuddy.Core.Library.Notifications.Dtos;
using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Core.Library.Users.Enums;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.User.Service.Services;
using FirebaseAdmin.Messaging;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserFollowedConsumer(IUserService userService, IPublishEndpoint publishEndpoint) : IConsumer<UserFollowedMessage>
    {
        private readonly IUserService _userService = userService;
        private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

        public async Task Consume(ConsumeContext<UserFollowedMessage> context)
        {
            string targetUserId = context.Message.TargetUserId;
            IEnumerable<string> tokens = await _userService.GetUserFCMTokensAsync(targetUserId);
            if (!tokens.Any())
                return;

            UserDto user = await _userService.GetUserByIdAsync(context.Message.UserId, UserExpandType.None);
            (string title, string body) = GetNewFollowerNotification(user);

            Notification notification = new NotificationBuilder()
                                            .SetTitle(title)
                                            .SetBody(body).Build();

            SendNotificationRequestMessage sendNotificationRequestMessage = new() { Notification = notification, Tokens = tokens };
            await _publishEndpoint.Publish(sendNotificationRequestMessage);
        }

        private static (string Title, string Body) GetNewFollowerNotification(UserDto user)
        {
            List<(string Title, string Body)> messages =
            [
                ("👋 New Connection!", $"{user.Username} just followed you. Say hi!"),
                ("🌟 You're Trending!", $"{user.Username} hit that follow button. Big vibes!"),
                ("✨ New Fan Alert!", $"{user.Username} just became a fan – nice!"),
                ("📣 Heads Up!", $"{user.Username} found you and hit follow."),
                ("🙌 Welcome Aboard!", $"{user.Username} just joined your follower crew."),
                ("🧭 Fresh Follower!", $"{user.Username} found their way to your profile."),
                ("🎉 Just Joined!", $"{user.Username} is now following you. Woohoo!"),
                ("🔥 It's Heating Up!", $"{user.Username} just followed – you're on fire!"),
                ("🤝 New Supporter!", $"{user.Username} has your back now."),
                ("🚀 Boost Incoming!", $"{user.Username} just leveled up your follower count.")
            ];
            return messages[ThreadSafeRandom.Global.Next(messages.Count)];
        }
    }
}
