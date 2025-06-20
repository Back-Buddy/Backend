using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class HasUserStrongRelationConsumer(IUserRelationService userRelationService) : IConsumer<HasUserStrongRelationRequestMessage>
    {
        private readonly IUserRelationService _userRelationService = userRelationService;

        public async Task Consume(ConsumeContext<HasUserStrongRelationRequestMessage> context)
        {
            bool hasStrongRelation = await _userRelationService.HasStrongRelation(context.Message.UserId, context.Message.TargetUserId);
            await context.RespondAsync(new HasUserStrongRelationResponseMessage { HasStrongRelation = hasStrongRelation });
        }
    }
}