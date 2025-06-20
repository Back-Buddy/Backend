using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserAddRelationConsumer(IUserRelationService userRelationService, ILogger<UserAddRelationConsumer> logger) : IConsumer<UserAddRelationRequestMessage>
    {
        private readonly IUserRelationService _userRelationService = userRelationService;
        private readonly ILogger<UserAddRelationConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserAddRelationRequestMessage> context)
        {
            _logger.LogDebug("Processing UserAddRelationRequestMessage for user: {UserId}, target: {TargetUserId}",
                context.Message.UserId, context.Message.TargetUserId);

            await _userRelationService.AddRelation(context.Message.UserId, context.Message.TargetUserId);

            await context.RespondAsync(new UserAddRelationResponseMessage());
        }
    }
}
