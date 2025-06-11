using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Dtos.Http;
using BackBuddy.Api.Service.V1.Users.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackBuddy.Api.Service.V1.Users.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController(IUserService userService) : ControllerBase
    {
        private readonly IUserService _userService = userService;

        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUser([FromQuery] SearchUserQueryDto query)
        {
            IEnumerable<UserDto> user = await _userService.SearchUser(query);
            return Ok(user);
        }
    }
}
