using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Auth
{
    public class TestConsumer : IConsumer<WebSocketMessageReceive>
    {
        public Task Consume(ConsumeContext<WebSocketMessageReceive> context)
        {
            //TODO: Implement the consumer logic
            throw new NotImplementedException();
        }
    }
}
