using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.DTOs.Http;
using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace BackBuddy.Api.Service.V1.Device.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Device/{deviceId:guid}/[controller]")]
    public class DeviceLogController(IDeviceLogService deviceLogService) : ControllerBase
    {
        private readonly IDeviceLogService _deviceLogService = deviceLogService;

        [HttpGet("{logId:guid}")]
        [ProducesResponseType(typeof(DeviceLogDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLog([FromRoute] Guid deviceId, [FromRoute] Guid logId)
        {
            DeviceLogDto deviceLog = await _deviceLogService.GetDeviceLog(this.GetUserId(), deviceId, logId);
            return Ok(deviceLog);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DeviceLogDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogs([FromRoute] Guid deviceId, [FromQuery] DeviceLogQueryDto queryParams, [FromQuery] PageRequestDto pageParams)
        {
            Page<List<DeviceLogDto>> deviceLogs = await _deviceLogService.GetDeviceLogs(this.GetUserId(), deviceId, queryParams, pageParams);

            Response.AddPageHeader(deviceLogs.HasMoreEntries);
            return Ok(deviceLogs.Items);
        }
    }
}
