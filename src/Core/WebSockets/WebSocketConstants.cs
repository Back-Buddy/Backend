using BackBuddy.Core.Library.WebSockets.Converter;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackBuddy.Core.Library.WebSockets
{
    public static class WebSocketConstants
    {
        public readonly static JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new WebSocketMessageConverter(), new JsonStringEnumConverter() }
        };
    }
}