using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Core.Library.Device.Dtos;
using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Device.Dtos.Queue.Report;
using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Core.Library.Device.Enums;
using BackBuddy.Core.Library.Users.Dtos;
using BackBuddy.Core.Library.Users.Dtos.Messages;
using BackBuddy.Core.Library.Utilities;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace BackBuddy.Api.Service.V1.Devices
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ReportController(
        IRequestClient<ReportCreateRequestMessage> reportCreateRequestClient,
        IRequestClient<ReportUpdateRequestMessage> reportUpdateRequestClient,
        IRequestClient<ReportDeleteRequestMessage> reportDeleteRequestClient,
        IRequestClient<ReportGetRequestMessage> reportGetRequestClient,
        IRequestClient<ReportGetReportsRequestMessage> reportGetReportsRequestClient,
        IRequestClient<ReportGetFeedRequestMessage> reportGetFeedRequestClient,
        IRequestClient<ReportGetEntityRequestMessage> reportGetEntityRequestClient,
        IRequestClient<ReportGetVisibilityTypeForUserRequestMessage> reportGetVisibilityTypeForUserRequestClient,
        IRequestClient<ReportAddLikeRequestMessage> reportAddLikeRequestClient,
        IRequestClient<ReportGetLikesFromReportRequestMessage> reportGetLikesFromReportRequestClient,
        IRequestClient<UserGetByIdRequestMessage> userGetByIdRequestClient) : ControllerBase
    {
        private readonly IRequestClient<ReportCreateRequestMessage> _reportCreateRequestClient = reportCreateRequestClient;
        private readonly IRequestClient<ReportUpdateRequestMessage> _reportUpdateRequestClient = reportUpdateRequestClient;
        private readonly IRequestClient<ReportDeleteRequestMessage> _reportDeleteRequestClient = reportDeleteRequestClient;
        private readonly IRequestClient<ReportGetRequestMessage> _reportGetRequestClient = reportGetRequestClient;
        private readonly IRequestClient<ReportGetReportsRequestMessage> _reportGetReportsRequestClient = reportGetReportsRequestClient;
        private readonly IRequestClient<ReportGetFeedRequestMessage> _reportGetFeedRequestClient = reportGetFeedRequestClient;
        private readonly IRequestClient<ReportGetEntityRequestMessage> _reportGetEntityRequestClient = reportGetEntityRequestClient;
        private readonly IRequestClient<ReportGetVisibilityTypeForUserRequestMessage> _reportGetVisibilityTypeForUserRequestClient = reportGetVisibilityTypeForUserRequestClient;
        private readonly IRequestClient<ReportAddLikeRequestMessage> _reportAddLikeRequestClient = reportAddLikeRequestClient;
        private readonly IRequestClient<ReportGetLikesFromReportRequestMessage> _reportGetLikesFromReportRequestClient = reportGetLikesFromReportRequestClient;
        private readonly IRequestClient<UserGetByIdRequestMessage> _userGetByIdRequestClient = userGetByIdRequestClient;

        [HttpPost]
        [ProducesResponseType(typeof(ReportDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateReport([FromBody] ReportCreateDto request)
        {
            ReportCreateRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                Request = request
            };

            Response<ReportCreateResponseMessage> response = await _reportCreateRequestClient.GetResponse<ReportCreateResponseMessage>(requestMessage);
            return CreatedAtAction(nameof(GetReport), new { reportId = response.Message.Report.Id }, response.Message.Report);
        }

        [HttpPatch("{reportId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateReport([FromRoute] Guid reportId, [FromBody] ReportUpdateDto request)
        {
            ReportUpdateRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                ReportId = reportId,
                Request = request
            };

            await _reportUpdateRequestClient.GetResponse<ReportUpdateResponseMessage>(requestMessage);
            return NoContent();
        }

        [HttpDelete("{reportId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteReport([FromRoute] Guid reportId, CancellationToken cancellationToken = default)
        {
            ReportDeleteRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                ReportId = reportId
            };

            await _reportDeleteRequestClient.GetResponse<ReportDeleteResponseMessage>(requestMessage, cancellationToken);
            return NoContent();
        }

        [HttpGet("{reportId:guid}")]
        [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReport([FromRoute] Guid reportId, [FromQuery][DefaultValue(ReportExpandType.None)] ReportExpandType expandType = ReportExpandType.None)
        {
            ReportGetRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                ReportId = reportId,
                ExpandType = expandType
            };

            Response<ReportGetResponseMessage> response = await _reportGetRequestClient.GetResponse<ReportGetResponseMessage>(requestMessage);
            return Ok(response.Message.Report);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReports([FromQuery] ReportQueryDto query, [FromQuery] PageRequestDto pageQuery, [FromQuery][DefaultValue(ReportExpandType.None)] ReportExpandType expandType = ReportExpandType.None)
        {
            ReportGetReportsRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                Query = query,
                Page = pageQuery,
                ExpandType = expandType
            };

            Response<ReportGetReportsResponseMessage> response = await _reportGetReportsRequestClient.GetResponse<ReportGetReportsResponseMessage>(requestMessage);
            Response.AddPageHeader(response.Message.Reports.HasMoreEntries);
            return Ok(response.Message.Reports.Items);
        }

        [HttpGet]
        [Route("feed")]
        [ProducesResponseType(typeof(List<ReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportFeed([FromQuery] ReportFeedQueryDto feedQuery, [FromQuery] PageRequestDto pageQuery)
        {
            ReportGetFeedRequestMessage requestMessage = new()
            {
                UserId = this.GetUserId(),
                Query = feedQuery,
                Page = pageQuery
            };

            Response<ReportGetFeedResponseMessage> response = await _reportGetFeedRequestClient.GetResponse<ReportGetFeedResponseMessage>(requestMessage);
            Response.AddPageHeader(response.Message.Reports.HasMoreEntries);
            return Ok(response.Message.Reports.Items);
        }

        [HttpPut]
        [Route("{reportId:guid}/like")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> LikeReport([FromRoute] Guid reportId, CancellationToken cancellationToken = default)
        {
            ReportGetEntityRequestMessage entityRequestMessage = new()
            {
                ReportId = reportId
            };

            Response<ReportGetEntityResponseMessage> entityResponse = await _reportGetEntityRequestClient.GetResponse<ReportGetEntityResponseMessage>(entityRequestMessage, cancellationToken);
            ReportEntity reportEntity = entityResponse.Message.Report;

            ReportGetVisibilityTypeForUserRequestMessage visibilityRequestMessage = new()
            {
                UserId = this.GetUserId(),
                TargetReport = reportEntity
            };

            Response<ReportGetVisibilityTypeForUserResponseMessage> visibilityResponse = await _reportGetVisibilityTypeForUserRequestClient.GetResponse<ReportGetVisibilityTypeForUserResponseMessage>(visibilityRequestMessage, cancellationToken);
            IEnumerable<ReportVisibilityType> visibilityTypes = visibilityResponse.Message.VisibilityTypes;

            ReportAddLikeRequestMessage likeRequestMessage = new()
            {
                UserId = this.GetUserId(),
                Report = reportEntity,
                VisibilityTypes = visibilityTypes
            };

            await _reportAddLikeRequestClient.GetResponse<ReportAddLikeResponseMessage>(likeRequestMessage, cancellationToken);
            return NoContent();
        }

        [HttpGet]
        [Route("{reportId:guid}/likes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLikes([FromRoute] Guid reportId, [FromQuery] PageRequestDto pageQuery, CancellationToken cancellationToken = default)
        {
            ReportGetEntityRequestMessage entityRequestMessage = new()
            {
                ReportId = reportId
            };

            Response<ReportGetEntityResponseMessage> entityResponse = await _reportGetEntityRequestClient.GetResponse<ReportGetEntityResponseMessage>(entityRequestMessage, cancellationToken);
            ReportEntity reportEntity = entityResponse.Message.Report;

            ReportGetVisibilityTypeForUserRequestMessage visibilityRequestMessage = new()
            {
                UserId = this.GetUserId(),
                TargetReport = reportEntity
            };

            Response<ReportGetVisibilityTypeForUserResponseMessage> visibilityResponse = await _reportGetVisibilityTypeForUserRequestClient.GetResponse<ReportGetVisibilityTypeForUserResponseMessage>(visibilityRequestMessage, cancellationToken);
            IEnumerable<ReportVisibilityType> visibilityTypes = visibilityResponse.Message.VisibilityTypes;

            ReportGetLikesFromReportRequestMessage likesRequestMessage = new()
            {
                Report = reportEntity,
                VisibilityTypes = visibilityTypes,
                Page = pageQuery
            };

            Response<ReportGetLikesFromReportResponseMessage> likesResponse = await _reportGetLikesFromReportRequestClient.GetResponse<ReportGetLikesFromReportResponseMessage>(likesRequestMessage, cancellationToken);
            Page<List<string>> result = likesResponse.Message.Likes;

            List<UserDto> users = [];

            IEnumerable<string[]> chunked = result.Items.Chunk(10);
            foreach (string[] chunk in chunked)
            {
                IEnumerable<Task<UserDto>> userTaks = chunk.Select(async userId =>
                {
                    UserGetByIdRequestMessage userRequestMessage = new()
                    {
                        UserId = userId,
                        UserExpandType = Core.Library.Users.Enums.UserExpandType.None
                    };

                    Response<UserGetByIdResponseMessage> userResponse = await _userGetByIdRequestClient.GetResponse<UserGetByIdResponseMessage>(userRequestMessage, cancellationToken);
                    return userResponse.Message.User;
                });

                UserDto[] userDtos = await Task.WhenAll(userTaks);
                users.AddRange(userDtos);
            }
            Response.AddPageHeader(result.HasMoreEntries);
            return Ok(users);
        }
    }
}