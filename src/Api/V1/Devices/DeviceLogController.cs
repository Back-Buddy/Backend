using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace BackBuddy.Api.Service.V1.Devices
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Device/{deviceId:guid}/[controller]")]
    public class DeviceLogController() : ControllerBase
    {
        [HttpGet("{logId:guid}")]
        [ProducesResponseType(typeof(DeviceLogDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLog([FromRoute] Guid deviceId, [FromRoute] Guid logId)
        {
            DeviceLogDto deviceLog = await _deviceLogService.GetDeviceLog(this.GetUserId(), deviceId, logId);
            return Ok(deviceLog);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DeviceLogDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogs([FromRoute] Guid deviceId, [FromQuery] DeviceLogQueryDto queryParams, [FromQuery] PageRequestDto pageQuery)
        {
            Page<List<DeviceLogDto>> deviceLogs = await _deviceLogService.GetDeviceLogs(this.GetUserId(), deviceId, queryParams, pageQuery);

            Response.AddPageHeader(deviceLogs.HasMoreEntries);
            return Ok(deviceLogs.Items);
        }
    }
}
