using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Device.Dtos.Queue;
using BackBuddy.Core.Library.Utilities;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace BackBuddy.Api.Service.V1.Devices
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DeviceController(
        IRequestClient<DeviceGetRequestMessage> deviceGetRequestMessage,
        IRequestClient<DeviceGetAllRequestMessage> deviceGetAllRequestMessage,
        IRequestClient<DeviceCreateRequestMessage> deviceCreateRequestMessage,
        IRequestClient<DeviceUpdateRequestMessage> deviceUpdateRequestMessage,
        IRequestClient<DeviceDeleteRequestMessage> deviceDeleteRequestMessage) : ControllerBase
    {
        private readonly IRequestClient<DeviceGetRequestMessage> _deviceGetRequestMessage = deviceGetRequestMessage;
        private readonly IRequestClient<DeviceGetAllRequestMessage> _deviceGetAllRequestMessage = deviceGetAllRequestMessage;
        private readonly IRequestClient<DeviceCreateRequestMessage> _deviceCreateRequestMessage = deviceCreateRequestMessage;
        private readonly IRequestClient<DeviceUpdateRequestMessage> _deviceUpdateRequestMessage = deviceUpdateRequestMessage;
        private readonly IRequestClient<DeviceDeleteRequestMessage> _deviceDeleteRequestMessage = deviceDeleteRequestMessage;

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDevice(Guid id)
        {
            DeviceGetRequestMessage requestMessage = new()
            {
                DeviceId = id,
                UserId = this.GetUserId()
            };

            Response<DeviceGetResponseMessage> response = await _deviceGetRequestMessage.GetResponse<DeviceGetResponseMessage>(requestMessage);
            return Ok(response.Message.Device);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DeviceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDevices([FromQuery] PageRequestDto pageQuery, [FromQuery] DeviceQueryDto queryParams)
        {
            DeviceGetAllRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                Page = pageQuery,
                Query = queryParams
            };

            Response<DeviceGetAllResponseMessage> response = await _deviceGetAllRequestMessage.GetResponse<DeviceGetAllResponseMessage>(requestMessage);
            Response.AddPageHeader(response.Message.Devices.HasMoreEntries);
            return Ok(response.Message.Devices.Items);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DeviceSecretDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateDevice([FromBody] DeviceCreateRequestDto request)
        {
            DeviceCreateRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                Request = request
            };

            Response<DeviceCreateResponseMessage> response = await _deviceCreateRequestMessage.GetResponse<DeviceCreateResponseMessage>(requestMessage);
            return CreatedAtAction(nameof(GetDevice), new { id = response.Message.DeviceSecret.DeviceId }, response.Message.DeviceSecret);
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateDevice(Guid id, [FromBody] DeviceUpdateRequestDto request)
        {
            DeviceUpdateRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                DeviceId = id,
                Request = request
            };

            await _deviceUpdateRequestMessage.GetResponse<DeviceUpdateResponseMessage>(requestMessage);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteDevice(Guid id)
        {
            DeviceDeleteRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                DeviceId = id
            };

            await _deviceDeleteRequestMessage.GetResponse<DeviceDeleteResponseMessage>(requestMessage);
            return NoContent();
        }
    }
}
