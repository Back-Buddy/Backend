using BackBuddy.Api.Service.V1.Users.Dtos.Messages;
using BackBuddy.Api.Service.V1.Users.Services;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Users.Consumers
{
    public class GetFcmTokensConsumer(IUserService userService) : IConsumer<GetFcmTokensRequestMessage>
    {
        private readonly IUserService _userService = userService;

        public async Task Consume(ConsumeContext<GetFcmTokensRequestMessage> context)
        {
            IEnumerable<string> tokens = await _userService.GetUserFCMTokensAsync(context.Message.UserId);

            GetFcmTokensResponseMessage response = new()
            {
                Tokens = tokens
            };
            await context.RespondAsync(response);
        }
    }
}
