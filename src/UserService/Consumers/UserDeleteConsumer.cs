using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserDeleteConsumer(IUserService userService, ILogger<UserDeleteConsumer> logger) : IConsumer<UserDeleteRequestMessage>
    {
        private readonly IUserService _userService = userService;
        private readonly ILogger<UserDeleteConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserDeleteRequestMessage> context)
        {
            _logger.LogDebug("Processing UserDeleteRequestMessage for user: {UserId}", context.Message.UserId);

            await _userService.DeleteUser(context.Message.UserId);

            await context.RespondAsync(new UserDeleteResponseMessage());
        }
    }
}
