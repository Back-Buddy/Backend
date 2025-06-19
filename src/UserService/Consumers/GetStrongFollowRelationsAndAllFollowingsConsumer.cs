using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.User.Service.Services;
using MassTransit;

namespace BackBuddy.User.Service.Consumers
{
    public class GetStrongFollowRelationsAndAllFollowingsConsumer(IUserRelationService userRelationService) : IConsumer<GetStrongFollowRelationsAndAllFollowingsRequestMessage>
    {
        private readonly IUserRelationService _userRelationService = userRelationService;

        public async Task Consume(ConsumeContext<GetStrongFollowRelationsAndAllFollowingsRequestMessage> context)
        {
            (IEnumerable<string> strongRelations, IEnumerable<string> following) = await _userRelationService.GetStrongFollowRelationsAndAllFollowings(context.Message.UserId);
            await context.RespondAsync(new GetStrongFollowRelationsAndAllFollowingsResponseMessage
            {
                StrongRelations = strongRelations,
                Following = following
            });
        }
    }
}