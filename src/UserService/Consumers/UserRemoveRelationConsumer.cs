using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserRemoveRelationConsumer(IUserRelationService userRelationService, ILogger<UserRemoveRelationConsumer> logger) : IConsumer<UserRemoveRelationRequestMessage>
    {
        private readonly IUserRelationService _userRelationService = userRelationService;
        private readonly ILogger<UserRemoveRelationConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserRemoveRelationRequestMessage> context)
        {
            _logger.LogDebug("Processing UserRemoveRelationRequestMessage for user: {UserId}, target: {TargetUserId}",
                context.Message.UserId, context.Message.TargetUserId);

            await _userRelationService.RemoveRelation(context.Message.UserId, context.Message.TargetUserId);

            await context.RespondAsync(new UserRemoveRelationResponseMessage());
        }
    }
}
