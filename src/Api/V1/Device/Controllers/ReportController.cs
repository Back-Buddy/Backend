using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.DTOs.Http;
using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Enums;
using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.Users.Dtos;
using BackBuddy.Api.Service.V1.Users.Services;
using BackBuddy.Api.Service.V1.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace BackBuddy.Api.Service.V1.Device.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ReportController(IReportService reportService, IReportLikeService reportLikeService, IUserService userService) : ControllerBase
    {
        private readonly IReportService _reportService = reportService;
        private readonly IReportLikeService _reportLikeService = reportLikeService;
        private readonly IUserService _userService = userService;

        [HttpPost]
        [ProducesResponseType(typeof(ReportDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateReport([FromBody] ReportCreateDto request)
        {
            ReportDto report = await _reportService.CreateReport(this.GetUserId(), request);
            return CreatedAtAction(nameof(GetReport), new { reportId = report.Id }, report);
        }

        [HttpPatch("{reportId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateReport([FromRoute] Guid reportId, [FromBody] ReportUpdateDto request)
        {
            await _reportService.UpdateReport(this.GetUserId(), reportId, request);
            return NoContent();
        }

        [HttpDelete("{reportId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteReport([FromRoute] Guid reportId, CancellationToken cancellationToken = default)
        {
            await _reportService.DeleteReport(this.GetUserId(), reportId, cancellationToken);
            return NoContent();
        }

        [HttpGet("{reportId:guid}")]
        [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReport([FromRoute] Guid reportId, [FromQuery][DefaultValue(ReportExpandType.None)] ReportExpandType expandType = ReportExpandType.None)
        {
            ReportDto report = await _reportService.GetReport(this.GetUserId(), reportId, expandType);
            return Ok(report);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReports([FromQuery] ReportQueryDto query, [FromQuery] PageRequestDto pageQuery, [FromQuery][DefaultValue(ReportExpandType.None)] ReportExpandType expandType = ReportExpandType.None)
        {
            Page<List<ReportDto>> reports = await _reportService.GetReports(this.GetUserId(), query, pageQuery, expandType);
            Response.AddPageHeader(reports.HasMoreEntries);
            return Ok(reports.Items);
        }

        [HttpGet]
        [Route("feed")]
        [ProducesResponseType(typeof(List<ReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportFeed([FromQuery] ReportFeedQueryDto feedQuery, [FromQuery] PageRequestDto pageQuery)
        {
            Page<List<ReportDto>> reports = await _reportService.GetReportFeed(this.GetUserId(), feedQuery, pageQuery);
            Response.AddPageHeader(reports.HasMoreEntries);
            return Ok(reports.Items);
        }

        [HttpPut]
        [Route("{reportId:guid}/like")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> LikeReport([FromRoute] Guid reportId, CancellationToken cancellationToken = default)
        {
            ReportEntity reportEntity = await _reportService.GetReportEntity(reportId, cancellationToken);
            IEnumerable<ReportVisibilityType> visibilityTypes = await _reportService.GetVisibilityTypeForUser(this.GetUserId(), reportEntity);

            await _reportLikeService.AddLike(this.GetUserId(), reportEntity, visibilityTypes, cancellationToken);
            return NoContent();
        }

        [HttpGet]
        [Route("{reportId:guid}/likes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLikes([FromRoute] Guid reportId, [FromQuery] PageRequestDto pageQuery, CancellationToken cancellationToken = default)
        {
            ReportEntity reportEntity = await _reportService.GetReportEntity(reportId, cancellationToken);
            IEnumerable<ReportVisibilityType> visibilityTypes = await _reportService.GetVisibilityTypeForUser(this.GetUserId(), reportEntity);

            Page<List<string>> result = await _reportLikeService.GetReportLikesFromReport(reportEntity, visibilityTypes, pageQuery, cancellationToken);

            IEnumerable<Task<UserDto>> tasks = result.Items.Select(x => _userService.GetUserByIdAsync(x));
            UserDto[] users = await Task.WhenAll(tasks);

            return Ok(users.ToList());
        }
    }
}