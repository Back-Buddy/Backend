using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserGetUserRelationConsumer(IUserRelationService userRelationService, ILogger<UserGetUserRelationConsumer> logger) : IConsumer<UserGetUserRelationRequestMessage>
    {
        private readonly IUserRelationService _userRelationService = userRelationService;
        private readonly ILogger<UserGetUserRelationConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserGetUserRelationRequestMessage> context)
        {
            try
            {
                _logger.LogDebug("Processing UserGetUserRelationRequestMessage for user: {UserId}, target: {TargetUserId}", 
                    context.Message.UserId, context.Message.TargetUserId);

                UserRelationDto relation = await _userRelationService.GetUserRelation(context.Message.UserId, context.Message.TargetUserId);

                await context.RespondAsync(new UserGetUserRelationResponseMessage
                {
                    Relation = relation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process UserGetUserRelationRequestMessage");
                throw;
            }
        }
    }
}
