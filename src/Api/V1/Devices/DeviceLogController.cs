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
    [Route("api/v{version:apiVersion}/Device/{deviceId:guid}/[controller]")]
    public class DeviceLogController(
        IRequestClient<DeviceGetDeviceLogRequestMessage> deviceGetDeviceLogRequestClient,
        IRequestClient<DeviceGetDeviceLogsRequestMessage> deviceGetDeviceLogsRequestClient) : ControllerBase
    {
        private readonly IRequestClient<DeviceGetDeviceLogRequestMessage> _deviceGetDeviceLogRequestClient = deviceGetDeviceLogRequestClient;
        private readonly IRequestClient<DeviceGetDeviceLogsRequestMessage> _deviceGetDeviceLogsRequestClient = deviceGetDeviceLogsRequestClient;

        [HttpGet("{logId:guid}")]
        [ProducesResponseType(typeof(DeviceLogDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLog([FromRoute] Guid deviceId, [FromRoute] Guid logId)
        {
            DeviceGetDeviceLogRequestMessage requestMessage = new DeviceGetDeviceLogRequestMessage
            {
                UserId = this.GetUserId(),
                DeviceId = deviceId,
                LogId = logId
            };

            Response<DeviceGetDeviceLogResponseMessage> response = await _deviceGetDeviceLogRequestClient.GetResponse<DeviceGetDeviceLogResponseMessage>(requestMessage);
            return Ok(response.Message.DeviceLog);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DeviceLogDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogs([FromRoute] Guid deviceId, [FromQuery] DeviceLogQueryDto queryParams, [FromQuery] PageRequestDto pageQuery)
        {
            DeviceGetDeviceLogsRequestMessage requestMessage = new DeviceGetDeviceLogsRequestMessage
            {
                UserId = this.GetUserId(),
                DeviceId = deviceId,
                Query = queryParams,
                Page = pageQuery
            };

            Response<DeviceGetDeviceLogsResponseMessage> response = await _deviceGetDeviceLogsRequestClient.GetResponse<DeviceGetDeviceLogsResponseMessage>(requestMessage);
            Response.AddPageHeader(response.Message.DeviceLogs.HasMoreEntries);
            return Ok(response.Message.DeviceLogs.Items);
        }
    }
}
