using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Auth
{
    public class TestConsumer(ILogger<TestConsumer> logger) : IConsumer<WebSocketMessageReceive<TestMessage>>
    {
        public Task Consume(ConsumeContext<WebSocketMessageReceive<TestMessage>> context)
        {
            logger.LogInformation(context.Message.Message.Status);
            return Task.CompletedTask;
        }
    }
}
