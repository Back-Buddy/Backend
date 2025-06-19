using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Core.Library.Utilities;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class UserGetOutgoingRelationsConsumer(IUserRelationService userRelationService, ILogger<UserGetOutgoingRelationsConsumer> logger) : IConsumer<UserGetOutgoingRelationsRequestMessage>
    {
        private readonly IUserRelationService _userRelationService = userRelationService;
        private readonly ILogger<UserGetOutgoingRelationsConsumer> _logger = logger;

        public async Task Consume(ConsumeContext<UserGetOutgoingRelationsRequestMessage> context)
        {
            try
            {
                _logger.LogDebug("Processing UserGetOutgoingRelationsRequestMessage for user: {UserId}", 
                    context.Message.UserId);

                Page<List<string>> relations = await _userRelationService.GetOutgoingRelations(
                    context.Message.UserId, context.Message.Page);

                await context.RespondAsync(new UserGetOutgoingRelationsResponseMessage
                {
                    Relations = relations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process UserGetOutgoingRelationsRequestMessage");
                throw;
            }
        }
    }
}
