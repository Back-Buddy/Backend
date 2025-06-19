using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Utilities;

namespace BackBuddy.Core.Library.Device.Dtos.Queue
{
    public record DeviceGetDeviceLogsRequestMessage
    {
        public required string UserId { get; init; }
        public required Guid DeviceId { get; init; }
        public required DeviceLogQueryDto Query { get; init; }
        public required PageRequestDto Page { get; init; }
    }

    public record DeviceGetDeviceLogsResponseMessage
    {
        public required Page<List<DeviceLogDto>> DeviceLogs { get; init; }
    }
}
