using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserSearchConsumer(IUserService userService, ILogger<UserSearchConsumer> logger) : IConsumer<UserSearchRequestMessage>
    {
        private readonly IUserService _userService = userService;
        private readonly ILogger<UserSearchConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserSearchRequestMessage> context)
        {
            try
            {
                _logger.LogDebug("Processing UserSearchRequestMessage with query: {Query}", context.Message.Query.SearchTerm);

                IEnumerable<UserDto> users = await _userService.SearchUser(context.Message.Query, context.Message.UserExpandType);

                await context.RespondAsync(new UserSearchResponseMessage
                {
                    Users = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process UserSearchRequestMessage");
                throw;
            }
        }
    }
}
