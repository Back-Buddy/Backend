using BackBuddy.Api.Service.V1.WebSockets.Services;
using BackBuddy.Core.Library.WebSockets.Dtos;
using MassTransit;

namespace BackBuddy.Api.Service.V1.WebSockets.Consumer
{
    public class WebSocketDeviceIsOnlineConsumer(IWebSocketService webSocketService) : IConsumer<WebSocketDeviceIsOnlineRequest>
    {
        private readonly IWebSocketService _webSocketService = webSocketService;

        public async Task Consume(ConsumeContext<WebSocketDeviceIsOnlineRequest> context)
        {
            bool isDeviceOnline = await _webSocketService.IsDeviceConnected(context.Message.DeviceId);
            await context.RespondAsync(new WebSocketDeviceIsOnlineResponse
            {
                IsOnline = isDeviceOnline
            });
        }
    }
}