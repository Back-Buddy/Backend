using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.Device.DTOs;
using BackBuddy.Api.Service.V1.Device.DTOs.Http;
using BackBuddy.Api.Service.V1.Device.Services;
using BackBuddy.Api.Service.V1.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace BackBuddy.Api.Service.V1.Device.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ReportController(IReportService reportService) : ControllerBase
    {
        private readonly IReportService _reportService = reportService;

        [HttpPost]
        [ProducesResponseType(typeof(ReportDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateReport([FromBody] ReportCreateDto request, CancellationToken cancellationToken = default)
        {
            ReportDto report = await _reportService.CreateReport(this.GetUserId(), request, cancellationToken);
            return CreatedAtAction(nameof(GetReport), new { reportId = report.Id }, report);
        }

        [HttpGet("{reportId:guid}")]
        [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReport(Guid reportId)
        {
            ReportDto report = await _reportService.GetReport(this.GetUserId(), reportId);
            return Ok(report);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReports([FromQuery] ReportQueryDto query, [FromQuery] PageRequestDto pageQuery)
        {
            Page<List<ReportDto>> reports = await _reportService.GetReports(this.GetUserId(), query, pageQuery);
            Response.AddPageHeader(reports.HasMoreEntries);
            return Ok(reports.Items);
        }

        [HttpDelete("{reportId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            await _reportService.DeleteReport(this.GetUserId(), reportId, cancellationToken);
            return NoContent();
        }
    }
}