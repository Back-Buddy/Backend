using BackBuddy.Api.Service.V1.Notifications.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos.Messages;
using BackBuddy.Api.Service.V1.Users.Enums;
using BackBuddy.Api.Service.V1.Users.Services;
using BackBuddy.Api.Service.V1.Utilities;
using FirebaseAdmin.Messaging;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Users.Consumers
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
                ("👋 Neue Verbindung!", $"{user.Username} folgt dir jetzt. Sag Hallo!"),
                ("🌟 Du bist im Trend!", $"{user.Username} hat dich gerade abonniert."),
                ("✨ Ein neuer Fan!", $"{user.Username} ist dir jetzt gefolgt – nice!"),
                ("📣 Aufmerksamkeit!", $"{user.Username} hat dich entdeckt und folgt dir."),
                ("🙌 Willkommen!", $"{user.Username} ist jetzt Teil deiner Follower."),
                ("🧭 Neuer Follower!", $"{user.Username} hat den Weg zu dir gefunden."),
                ("🎉 Frisch dabei!", $"{user.Username} folgt dir ab sofort."),
                ("🔥 Es wird heiß!", $"{user.Username} folgt dir jetzt – läuft bei dir!"),
                ("🤝 Neue Unterstützung!", $"{user.Username} steht jetzt hinter dir."),
                ("🚀 Wachstum!", $"{user.Username} boostet deine Follower-Zahl.")
            ];
            return messages[ThreadSafeRandom.Global.Next(messages.Count)];
        }
    }
}
