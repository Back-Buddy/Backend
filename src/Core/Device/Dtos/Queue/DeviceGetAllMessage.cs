using BackBuddy.Core.Library.Device.Dtos.Http;
using BackBuddy.Core.Library.Utilities;

namespace BackBuddy.Core.Library.Device.Dtos.Queue
{
    public record DeviceGetAllRequestMessage
    {
        public required string UserId { get; init; }
        public required PageRequestDto Page { get; init; }
        public required DeviceQueryDto Query { get; init; }
    }

    public record DeviceGetAllResponseMessage
    {
        public required Page<List<DeviceDto>> Devices { get; init; }
    }
}
