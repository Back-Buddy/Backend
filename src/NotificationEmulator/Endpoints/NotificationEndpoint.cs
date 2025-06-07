using BackBuddy.Core.Library.Notifications;
using BackBuddy.Notification.Emulator.Services;

namespace BackBuddy.Notification.Emulator.Endpoints
{
    public static class NotificationEndpoint
    {
        public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapDelete("clear", (INotificationService service) =>
            {
                service.Clear();
                return Results.Ok();
            }).Produces(StatusCodes.Status200OK);

            app.MapPost("send", (INotificationService service, NotificationDevDebugDto request) =>
            {
                service.AddNotification(request);
                return Results.Ok();
            }).Produces(StatusCodes.Status200OK);

            app.MapGet("cache", (INotificationService service) =>
            {
                return Results.Ok(service.GetNotifications());
            }).Produces<IEnumerable<NotificationDevDebugDto>>(StatusCodes.Status200OK);
        }
    }
}
