using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1
{
    [TestClass]
    public class TestClass
    {

        [TestMethod]
        public async Task TestMethod()
        {
            ClientWebSocket clientWebSocket = new();
            Uri uri = new("ws://localhost:8080/ws");
            clientWebSocket.Options.AddSubProtocol("dadsad");
            await clientWebSocket.ConnectAsync(uri, CancellationToken.None);

            JsonObject payload = new()
            {
                { "Status", "sit" }
            };

            JsonObject message = new()
            {
                { "messageType", "StatusMessage" },
                { "status", "sit"}
            };

            await clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message.ToJsonString())), WebSocketMessageType.Text, true, CancellationToken.None);

            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "sad", CancellationToken.None);
        }
    }
}
