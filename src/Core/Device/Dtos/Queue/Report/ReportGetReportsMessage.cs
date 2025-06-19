using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Device.Enums;
using BackBuddy.Core.Library.Utilities;

namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportGetReportsRequestMessage
    {
        public required string UserId { get; init; }
        public required ReportQueryDto Query { get; init; }
        public required PageRequestDto Page { get; init; }
        public required ReportExpandType ExpandType { get; init; }
    }

    public record ReportGetReportsResponseMessage
    {
        public required Page<List<ReportDto>> Reports { get; init; }
    }
}
