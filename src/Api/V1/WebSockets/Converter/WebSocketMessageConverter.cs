using BackBuddy.Api.Service.V1.WebSockets.DTOs;
using BackBuddy.Api.Service.V1.WebSockets.Enums;
using BackBuddy.Api.Service.V1.WebSockets.Exceptions;
using BackBuddy.Api.Service.V1.WebSockets.Mapper;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackBuddy.Api.Service.V1.WebSockets.Converter
{
    public class WebSocketMessageConverter : JsonConverter<IWebSocketMessageDto>
    {
        public override IWebSocketMessageDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            string rawType = GetProperty(doc.RootElement, "messagetype") ?? throw new InvalidWebSocketMessageException();
            if (!Enum.TryParse(rawType, out WebSocketMessageType type))
                throw new UnkownWebSocketMessageException(rawType);
            object obj = JsonSerializer.Deserialize(doc.RootElement.GetRawText(), type.GetMessageType(), options) ?? throw new InvalidWebSocketMessageException();
            return (IWebSocketMessageDto)obj;
        }

        public override void Write(Utf8JsonWriter writer, IWebSocketMessageDto value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.MessageType.GetMessageType(), options);
        }

        private static string? GetProperty(JsonElement element, string key)
        {
            return element.EnumerateObject()
                .FirstOrDefault(x => x.Name.Equals(key, StringComparison.OrdinalIgnoreCase)).Value.GetString();
        }
    }
}
