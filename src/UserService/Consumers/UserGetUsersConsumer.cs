using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserGetUsersConsumer(IUserService userService, ILogger<UserGetUsersConsumer> logger) : IConsumer<UserGetUsersRequestMessage>
    {
        private readonly IUserService _userService = userService;
        private readonly ILogger<UserGetUsersConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserGetUsersRequestMessage> context)
        {
            try
            {
                _logger.LogDebug("Processing UserGetUsersRequestMessage for {Count} users", context.Message.UserIds.Count);

                List<UserDto> users = await _userService.GetUsers(context.Message.UserIds, context.Message.UserExpandType);

                await context.RespondAsync(new UserGetUsersResponseMessage
                {
                    Users = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process UserGetUsersRequestMessage");
                throw;
            }
        }
    }
}
