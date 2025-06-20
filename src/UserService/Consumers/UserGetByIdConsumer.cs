using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserGetByIdConsumer(IUserService userService, ILogger<UserGetByIdConsumer> logger) : IConsumer<UserGetByIdRequestMessage>
    {
        private readonly IUserService _userService = userService;
        private readonly ILogger<UserGetByIdConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserGetByIdRequestMessage> context)
        {
            _logger.LogDebug("Processing UserGetByIdRequestMessage for user: {UserId}", context.Message.UserId);

            UserDto user = await _userService.GetUserByIdAsync(context.Message.UserId, context.Message.UserExpandType);

            await context.RespondAsync(new UserGetByIdResponseMessage
            {
                User = user
            });
        }
    }
}
