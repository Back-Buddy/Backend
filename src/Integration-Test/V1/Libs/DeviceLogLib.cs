using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.Extensions;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Libs
{
    internal class DeviceLogLib
    {
        private readonly HttpClient _httpClient;

        public DeviceLogLib(string baseUri)
        {
            _httpClient = new()
            {
                BaseAddress = new Uri(baseUri),
            };
        }

        public async Task<JsonObject> GetLog(string accessToken, Guid deviceId, Guid logId)
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"/api/v1/device/{deviceId}/DeviceLog/{logId}");
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
                throw new RequestFailedException(response);
            string content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonObject>(content);
        }

        public async Task<(JsonArray Logs, bool HasMoreEntries)> GetLogs(string accessToken, Guid deviceId, string logType = null, bool descending = true, DateTime? startTime = null, DateTime? endTime = null, int page = 1, int pageSize = 10)
        {
            string query = $"/api/v1/device/{deviceId}/DeviceLog?page={page}&size={pageSize}&descending={descending}";
            if (logType != null)
                query += $"&logType={logType}";
            if (startTime != null)
                query += $"&startTime={startTime.Value:o}";
            if (endTime != null)
                query += $"&endTime={endTime.Value:o}";

            HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, query);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
                throw new RequestFailedException(response);
            string content = await response.Content.ReadAsStringAsync();
            bool hasMoreEntries = bool.Parse(response.Headers.GetValues("X-Has-More-Entries").First().ToString());
            return (JsonSerializer.Deserialize<JsonArray>(content), hasMoreEntries);
        }

        public static async Task CreateSampleLogs(string websocketUri, string secret, int successCount = 0, int errorCount = 0, TimeSpan? delay = null)
        {
            using ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.AddSubProtocol(secret);
            await clientWebSocket.ConnectAsync(new Uri(websocketUri), CancellationToken.None);

            for (int i = 0; i < successCount; i++)
            {
                JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
                await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);
                await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);

                if(delay != null)
                    await Task.Delay(delay.Value);

                JsonObject standingStatus = DeviceLib.CreateUpdateStatus("Standing");
                await clientWebSocket.SendAsync(standingStatus, int.MaxValue, CancellationToken.None);
                await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);
            }

            // Set default status to "Sitting" for errorCount
            if (errorCount > 0)
            {
                JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
                await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);
                await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 2, CancellationToken.None);
            }

            for (int i = 0; i < errorCount; i++)
            {
                JsonObject sittingStatus = DeviceLib.CreateUpdateStatus("Sitting");
                await clientWebSocket.SendAsync(sittingStatus, int.MaxValue, CancellationToken.None);
                await clientWebSocket.PollMessage("DeviceUpdateStatusAck", 1, CancellationToken.None);

                if (delay != null)
                    await Task.Delay(delay.Value);
            }

            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
        }
    }
}
