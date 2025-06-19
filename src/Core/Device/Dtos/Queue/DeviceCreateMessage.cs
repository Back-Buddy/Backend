using BackBuddy.Core.Library.Device.Dtos.Http;

namespace BackBuddy.Core.Library.Device.Dtos.Queue
{
    public record DeviceCreateRequestMessage
    {
        public required string UserId { get; init; }
        public required DeviceCreateRequestDto Request { get; init; }
    }

    public record DeviceCreateResponseMessage
    {
        public required DeviceSecretDto DeviceSecret { get; init; }
    }
}
