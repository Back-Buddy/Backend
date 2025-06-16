using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos.Messages;
using BackBuddy.Api.Service.V1.Users.Services;
using MassTransit;

namespace BackBuddy.Api.Service.V1.Users.Consumers
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
