using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class GetUserConsumer(IUserService userService) : IConsumer<GetUserRequestMessage>
    {
        private readonly IUserService _userService = userService;

        public async Task Consume(ConsumeContext<GetUserRequestMessage> context)
        {
            UserDto user = await _userService.GetUserByIdAsync(context.Message.UserId);
            await context.RespondAsync(new GetUserResponseMessage { User = user });
        }
    }
}
