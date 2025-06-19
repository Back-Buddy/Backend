using BackBuddy.Core.Library.Device.Dtos.Http;

namespace BackBuddy.Core.Library.Device.Dtos.Queue
{
    public record DeviceUpdateRequestMessage
    {
        public required string UserId { get; init; }
        public required Guid DeviceId { get; init; }
        public required DeviceUpdateRequestDto Request { get; init; }
    }

    public record DeviceUpdateResponseMessage
    {
        // Simple response object with no content
    }
}
