using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
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
