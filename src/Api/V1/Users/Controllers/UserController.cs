using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos.Http;
using BackBuddy.Api.Service.V1.Users.Exceptions;
using BackBuddy.Api.Service.V1.Users.Services;
using BackBuddy.Api.Service.V1.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace BackBuddy.Api.Service.V1.Users.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController(IUserService userService, IUserRelationService userRelationService) : ControllerBase
    {
        private readonly IUserService _userService = userService;
        private readonly IUserRelationService _userRelationService = userRelationService;

        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUser([FromQuery] SearchUserQueryDto query)
        {
            IEnumerable<UserDto> user = await _userService.SearchUser(query);
            return Ok(user);
        }

        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserById([FromRoute] string userId)
        {
            UserDto user = await _userService.GetUserByIdAsync(userId);
            return Ok(user);
        }

        [HttpGet("{userId}/followers")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFollowers([FromRoute] string userId, [FromQuery] PageRequestDto pageQuery)
        {
            if (!await _userService.IsUserIdValid(userId))
                throw new UserNotFoundException();

            Page<List<string>> followers = await _userRelationService.GetIncomingReleations(userId, pageQuery);
            List<UserDto> followersAsUser = await _userService.GetUsers(followers.Items);

            Response.AddPageHeader(followers.HasMoreEntries);

            return Ok(followersAsUser);
        }

        [HttpGet("{userId}/following")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFollowing([FromRoute] string userId, [FromQuery] PageRequestDto pageQuery)
        {
            if (!await _userService.IsUserIdValid(userId))
                throw new UserNotFoundException();

            Page<List<string>> following = await _userRelationService.GetOutgoingReleations(userId, pageQuery);
            List<UserDto> followingAsUser = await _userService.GetUsers(following.Items);

            Response.AddPageHeader(following.HasMoreEntries);

            return Ok(followingAsUser);
        }

        [HttpPut("{userId}/follow")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> FollowUser([FromRoute] string userId)
        {
            if (!await _userService.IsUserIdValid(userId))
                throw new UserNotFoundException();

            await _userRelationService.AddRelation(this.GetUserId(), userId);
            return NoContent();
        }

        [HttpDelete("{userId}/follow")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UnFollowUser([FromRoute] string userId)
        {
            if (!await _userService.IsUserIdValid(userId))
                throw new UserNotFoundException();

            await _userRelationService.RemoveRelation(this.GetUserId(), userId);
            return NoContent();
        }
    }
}
