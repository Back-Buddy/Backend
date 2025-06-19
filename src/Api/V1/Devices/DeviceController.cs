using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace BackBuddy.Api.Service.V1.Devices
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DeviceController() : ControllerBase
    {
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDevice(Guid id)
        {
            DeviceDto device = await deviceService.Get(this.GetUserId(), id);
            return Ok(device);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DeviceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDevices([FromQuery] PageRequestDto pageQuery, [FromQuery] DeviceQueryDto queryParams)
        {
            Page<List<DeviceDto>> devices = await deviceService.GetAll(this.GetUserId(), pageQuery, queryParams);
            Response.AddPageHeader(devices.HasMoreEntries);
            return Ok(devices.Items);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DeviceSecretDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateDevice([FromBody] DeviceCreateRequestDto request)
        {
            DeviceSecretDto deviceSecret = await deviceService.Create(this.GetUserId(), request);
            return CreatedAtAction(nameof(GetDevice), new { id = deviceSecret.DeviceId }, deviceSecret);
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateDevice(Guid id, [FromBody] DeviceUpdateRequestDto request)
        {
            await deviceService.Update(this.GetUserId(), id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteDevice(Guid id)
        {
            await deviceService.Delete(this.GetUserId(), id);
            return NoContent();
        }
    }
}
