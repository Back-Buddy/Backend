using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserGetIncomingRelationsConsumer(IUserRelationService userRelationService, ILogger<UserGetIncomingRelationsConsumer> logger) : IConsumer<UserGetIncomingRelationsRequestMessage>
    {
        private readonly IUserRelationService _userRelationService = userRelationService;
        private readonly ILogger<UserGetIncomingRelationsConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserGetIncomingRelationsRequestMessage> context)
        {
            _logger.LogDebug("Processing UserGetIncomingRelationsRequestMessage for user: {UserId}",
                context.Message.UserId);

            Page<List<string>> relations = await _userRelationService.GetIncomingRelations(
                context.Message.UserId, context.Message.Page);

            await context.RespondAsync(new UserGetIncomingRelationsResponseMessage
            {
                Relations = relations
            });
        }
    }
}
