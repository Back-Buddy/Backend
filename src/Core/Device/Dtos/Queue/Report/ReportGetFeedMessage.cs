using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Utilities;

namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportGetFeedRequestMessage
    {
        public required string UserId { get; init; }
        public required ReportFeedQueryDto Query { get; init; }
        public required PageRequestDto Page { get; init; }
    }

    public record ReportGetFeedResponseMessage
    {
        public required Page<List<ReportDto>> Reports { get; init; }
    }
}
