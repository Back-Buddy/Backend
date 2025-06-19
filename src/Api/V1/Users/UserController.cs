using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Users.Exceptions;
using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Http;
using BackBuddy.Core.Library.Users.Enums;
using BackBuddy.Core.Library.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace BackBuddy.Api.Service.V1.Users
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController() : ControllerBase
    {
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteUser()
        {
            await _userService.DeleteUser(this.GetUserId());
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUser([FromQuery] SearchUserQueryDto query, [FromQuery][DefaultValue(UserExpandType.None)] UserExpandType expandType)
        {
            IEnumerable<UserDto> users = await _userService.SearchUser(query, expandType);
            return Ok(users);
        }

        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserById([FromRoute] string userId, [FromQuery][DefaultValue(UserExpandType.None)] UserExpandType expandType)
        {
            UserDto user = await _userService.GetUserByIdAsync(userId, expandType);
            return Ok(user);
        }

        [HttpGet("{userId}/relation")]
        [ProducesResponseType(typeof(UserRelationDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserRelation([FromRoute] string userId)
        {
            if (!await _userService.IsUserIdValid(userId))
                throw new UserNotFoundException();

            UserRelationDto relation = await _userRelationService.GetUserRelation(this.GetUserId(), userId);
            return Ok(relation);
        }

        [HttpGet("{userId}/followers")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFollowers([FromRoute] string userId, [FromQuery] PageRequestDto pageQuery, [FromQuery][DefaultValue(UserExpandType.None)] UserExpandType expandType = UserExpandType.None)
        {
            if (!await _userService.IsUserIdValid(userId))
                throw new UserNotFoundException();

            Page<List<string>> followers = await _userRelationService.GetIncomingRelations(userId, pageQuery);
            List<UserDto> followersAsUser = await _userService.GetUsers(followers.Items, expandType);

            Response.AddPageHeader(followers.HasMoreEntries);

            return Ok(followersAsUser);
        }

        [HttpGet("{userId}/following")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFollowing([FromRoute] string userId, [FromQuery] PageRequestDto pageQuery, [FromQuery][DefaultValue(UserExpandType.None)] UserExpandType expandType = UserExpandType.None)
        {
            if (!await _userService.IsUserIdValid(userId))
                throw new UserNotFoundException();

            Page<List<string>> following = await _userRelationService.GetOutgoingRelations(userId, pageQuery);
            List<UserDto> followingAsUser = await _userService.GetUsers(following.Items, expandType);

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
