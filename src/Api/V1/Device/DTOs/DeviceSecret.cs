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
            return Convert.ToBase64String(jsonBytes).TrimEnd('=');
        }

        public static DeviceSecret Decode(string base64)
        {
            int padding = 4 - (base64.Length % 4);
            if (padding < 4)
                base64 += new string('=', padding);

            DeviceSecret secret = JsonSerializer.Deserialize<DeviceSecret>(Convert.FromBase64String(base64)) ?? throw new JsonException();
            return secret;
        }

    }
}
