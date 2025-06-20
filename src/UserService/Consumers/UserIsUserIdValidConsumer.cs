using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserIsUserIdValidConsumer(IUserService userService, ILogger<UserIsUserIdValidConsumer> logger) : IConsumer<UserIsUserIdValidRequestMessage>
    {
        private readonly IUserService _userService = userService;
        private readonly ILogger<UserIsUserIdValidConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserIsUserIdValidRequestMessage> context)
        {
            _logger.LogDebug("Processing UserIsUserIdValidRequestMessage for user: {UserId}", context.Message.UserId);

            bool isValid = await _userService.IsUserIdValid(context.Message.UserId);

            await context.RespondAsync(new UserIsUserIdValidResponseMessage
            {
                IsValid = isValid
            });
        }
    }
}
