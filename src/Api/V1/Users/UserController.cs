using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Users.Exceptions;
using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Http;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Core.Library.Users.Enums;
using BackBuddy.Core.Library.Utilities;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace BackBuddy.Api.Service.V1.Users
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController(
        IRequestClient<UserDeleteRequestMessage> userDeleteRequestClient,
        IRequestClient<UserSearchRequestMessage> userSearchRequestClient,
        IRequestClient<UserGetByIdRequestMessage> userGetByIdRequestClient,
        IRequestClient<UserGetUserRelationRequestMessage> userGetUserRelationRequestClient,
        IRequestClient<UserIsUserIdValidRequestMessage> userIsUserIdValidRequestClient,
        IRequestClient<UserGetIncomingRelationsRequestMessage> userGetIncomingRelationsRequestClient,
        IRequestClient<UserGetOutgoingRelationsRequestMessage> userGetOutgoingRelationsRequestClient,
        IRequestClient<UserGetUsersRequestMessage> userGetUsersRequestClient,
        IRequestClient<UserAddRelationRequestMessage> userAddRelationRequestClient,
        IRequestClient<UserRemoveRelationRequestMessage> userRemoveRelationRequestClient) : ControllerBase
    {
        private readonly IRequestClient<UserDeleteRequestMessage> _userDeleteRequestClient = userDeleteRequestClient;
        private readonly IRequestClient<UserSearchRequestMessage> _userSearchRequestClient = userSearchRequestClient;
        private readonly IRequestClient<UserGetByIdRequestMessage> _userGetByIdRequestClient = userGetByIdRequestClient;
        private readonly IRequestClient<UserGetUserRelationRequestMessage> _userGetUserRelationRequestClient = userGetUserRelationRequestClient;
        private readonly IRequestClient<UserIsUserIdValidRequestMessage> _userIsUserIdValidRequestClient = userIsUserIdValidRequestClient;
        private readonly IRequestClient<UserGetIncomingRelationsRequestMessage> _userGetIncomingRelationsRequestClient = userGetIncomingRelationsRequestClient;
        private readonly IRequestClient<UserGetOutgoingRelationsRequestMessage> _userGetOutgoingRelationsRequestClient = userGetOutgoingRelationsRequestClient;
        private readonly IRequestClient<UserGetUsersRequestMessage> _userGetUsersRequestClient = userGetUsersRequestClient;
        private readonly IRequestClient<UserAddRelationRequestMessage> _userAddRelationRequestClient = userAddRelationRequestClient;
        private readonly IRequestClient<UserRemoveRelationRequestMessage> _userRemoveRelationRequestClient = userRemoveRelationRequestClient;

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteUser()
        {
            UserDeleteRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId()
            };

            await _userDeleteRequestClient.GetResponse<UserDeleteResponseMessage>(requestMessage);
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUser([FromQuery] SearchUserQueryDto query, [FromQuery][DefaultValue(UserExpandType.None)] UserExpandType expandType)
        {
            UserSearchRequestMessage requestMessage = new()
            {
                Query = query,
                UserExpandType = expandType
            };

            Response<UserSearchResponseMessage> response = await _userSearchRequestClient.GetResponse<UserSearchResponseMessage>(requestMessage);
            return Ok(response.Message.Users);
        }

        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserById([FromRoute] string userId, [FromQuery][DefaultValue(UserExpandType.None)] UserExpandType expandType)
        {
            UserGetByIdRequestMessage requestMessage = new()
            {
                UserId = userId,
                UserExpandType = expandType
            };

            Response<UserGetByIdResponseMessage> response = await _userGetByIdRequestClient.GetResponse<UserGetByIdResponseMessage>(requestMessage);
            return Ok(response.Message.User);
        }

        [HttpGet("{userId}/relation")]
        [ProducesResponseType(typeof(UserRelationDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserRelation([FromRoute] string userId)
        {
            // Check if user exists
            UserIsUserIdValidRequestMessage userValidRequestMessage = new()
            {
                UserId = userId
            };

            Response<UserIsUserIdValidResponseMessage> userValidResponse = await _userIsUserIdValidRequestClient.GetResponse<UserIsUserIdValidResponseMessage>(userValidRequestMessage);
            if (!userValidResponse.Message.IsValid)
                throw new UserNotFoundException();

            // Get relation
            UserGetUserRelationRequestMessage relationRequestMessage = new()
            {
                UserId = this.GetUserId(),
                TargetUserId = userId
            };

            Response<UserGetUserRelationResponseMessage> relationResponse = await _userGetUserRelationRequestClient.GetResponse<UserGetUserRelationResponseMessage>(relationRequestMessage);
            return Ok(relationResponse.Message.Relation);
        }

        [HttpGet("{userId}/followers")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFollowers([FromRoute] string userId, [FromQuery] PageRequestDto pageQuery, [FromQuery][DefaultValue(UserExpandType.None)] UserExpandType expandType = UserExpandType.None)
        {
            // Check if user exists
            UserIsUserIdValidRequestMessage userValidRequestMessage = new()
            {
                UserId = userId
            };

            Response<UserIsUserIdValidResponseMessage> userValidResponse = await _userIsUserIdValidRequestClient.GetResponse<UserIsUserIdValidResponseMessage>(userValidRequestMessage);
            if (!userValidResponse.Message.IsValid)
                throw new UserNotFoundException();

            // Get followers
            UserGetIncomingRelationsRequestMessage incomingRequestMessage = new()
            {
                UserId = userId,
                Page = pageQuery
            };

            Response<UserGetIncomingRelationsResponseMessage> incomingResponse = await _userGetIncomingRelationsRequestClient.GetResponse<UserGetIncomingRelationsResponseMessage>(incomingRequestMessage);
            Page<List<string>> followers = incomingResponse.Message.Relations;

            // Get user details
            UserGetUsersRequestMessage usersRequestMessage = new()
            {
                UserIds = followers.Items,
                UserExpandType = expandType
            };

            Response<UserGetUsersResponseMessage> usersResponse = await _userGetUsersRequestClient.GetResponse<UserGetUsersResponseMessage>(usersRequestMessage);
            
            Response.AddPageHeader(followers.HasMoreEntries);
            return Ok(usersResponse.Message.Users);
        }

        [HttpGet("{userId}/following")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFollowing([FromRoute] string userId, [FromQuery] PageRequestDto pageQuery, [FromQuery][DefaultValue(UserExpandType.None)] UserExpandType expandType = UserExpandType.None)
        {
            // Check if user exists
            UserIsUserIdValidRequestMessage userValidRequestMessage = new()
            {
                UserId = userId
            };

            Response<UserIsUserIdValidResponseMessage> userValidResponse = await _userIsUserIdValidRequestClient.GetResponse<UserIsUserIdValidResponseMessage>(userValidRequestMessage);
            if (!userValidResponse.Message.IsValid)
                throw new UserNotFoundException();

            // Get following
            UserGetOutgoingRelationsRequestMessage outgoingRequestMessage = new()
            {
                UserId = userId,
                Page = pageQuery
            };

            Response<UserGetOutgoingRelationsResponseMessage> outgoingResponse = await _userGetOutgoingRelationsRequestClient.GetResponse<UserGetOutgoingRelationsResponseMessage>(outgoingRequestMessage);
            Page<List<string>> following = outgoingResponse.Message.Relations;

            // Get user details
            UserGetUsersRequestMessage usersRequestMessage = new()
            {
                UserIds = following.Items,
                UserExpandType = expandType
            };

            Response<UserGetUsersResponseMessage> usersResponse = await _userGetUsersRequestClient.GetResponse<UserGetUsersResponseMessage>(usersRequestMessage);
            
            Response.AddPageHeader(following.HasMoreEntries);
            return Ok(usersResponse.Message.Users);
        }

        [HttpPut("{userId}/follow")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> FollowUser([FromRoute] string userId)
        {
            // Check if user exists
            UserIsUserIdValidRequestMessage userValidRequestMessage = new()
            {
                UserId = userId
            };

            Response<UserIsUserIdValidResponseMessage> userValidResponse = await _userIsUserIdValidRequestClient.GetResponse<UserIsUserIdValidResponseMessage>(userValidRequestMessage);
            if (!userValidResponse.Message.IsValid)
                throw new UserNotFoundException();

            // Add relation
            UserAddRelationRequestMessage addRelationRequestMessage = new()
            {
                UserId = this.GetUserId(),
                TargetUserId = userId
            };

            await _userAddRelationRequestClient.GetResponse<UserAddRelationResponseMessage>(addRelationRequestMessage);
            return NoContent();
        }

        [HttpDelete("{userId}/follow")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UnFollowUser([FromRoute] string userId)
        {
            // Check if user exists
            UserIsUserIdValidRequestMessage userValidRequestMessage = new()
            {
                UserId = userId
            };

            Response<UserIsUserIdValidResponseMessage> userValidResponse = await _userIsUserIdValidRequestClient.GetResponse<UserIsUserIdValidResponseMessage>(userValidRequestMessage);
            if (!userValidResponse.Message.IsValid)
                throw new UserNotFoundException();

            // Remove relation
            UserRemoveRelationRequestMessage removeRelationRequestMessage = new()
            {
                UserId = this.GetUserId(),
                TargetUserId = userId
            };

            await _userRemoveRelationRequestClient.GetResponse<UserRemoveRelationResponseMessage>(removeRelationRequestMessage);
            return NoContent();
        }
    }
}
