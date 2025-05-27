using BackBuddy.Integration_Test.Exceptions;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.Extensions
{
    public static class ClientWebSocketExtension
    {

        public static async Task SendAsync<T>(this ClientWebSocket client, T payload, int bufferSize, CancellationToken cancellationToken, JsonSerializerOptions options = null) where T : class
        {
            string payloadString = JsonSerializer.Serialize(payload, options);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            IEnumerable<byte[]> chunkedPayload = payloadBytes.Chunk(bufferSize);
            foreach ((int index, byte[] buffer) in chunkedPayload.Index())
            {
                bool endOfMessage = index == chunkedPayload.Count() - 1;
                await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, endOfMessage, cancellationToken);
            }
        }

        public static async Task<JsonObject> PollMessage(this ClientWebSocket client, string messageType, int maxAttempts, CancellationToken cancellationToken)
        {
            int attempts = 0;
            do
            {
                WebSocketReceiveResult result;
                using MemoryStream ms = new();
                byte[] buffer = new byte[1024 * 4];
                do
                {
                    result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    await ms.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                } while (!result.EndOfMessage);

                JsonObject response = JsonSerializer.Deserialize<JsonObject>(ms.ToArray());
                if (response["MessageType"].GetValue<string>().Equals(messageType, StringComparison.InvariantCultureIgnoreCase))
                    return response;
                attempts++;
                Console.WriteLine($"Received message of type '{response["MessageType"].GetValue<string>()}', expected '{messageType}'. Attempt {attempts}/{maxAttempts}.");
            } while (maxAttempts > attempts && !cancellationToken.IsCancellationRequested);

            throw new PollFailedException();
        }
    }
}
