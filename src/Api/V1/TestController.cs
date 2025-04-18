using Microsoft.AspNetCore.Mvc;

namespace BackBuddy.Api.Service.V1
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TestController : ControllerBase
    {

        [HttpGet]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public IActionResult Test()
        {
            return Ok("Hello World");
        }
    }
}
