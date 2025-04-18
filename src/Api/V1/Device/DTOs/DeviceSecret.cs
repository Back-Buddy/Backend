using System.Text.Json;

namespace BackBuddy.Api.Service.V1.Device.DTOs
{
    public record DeviceSecret
    {
        public required Guid DeviceId { get; init; }
        public required string Secret { get; init; }

        public string Encode()
        {
            string rawJson = JsonSerializer.Serialize(this);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(rawJson);
            return Convert.ToBase64String(jsonBytes);
        }
    }
}
