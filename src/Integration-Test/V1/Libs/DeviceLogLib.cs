using BackBuddy.Integration_Test.Exceptions;
using System.Net.Http.Headers;
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

        public async Task<JsonArray> GetLogs(string accessToken, Guid deviceId, string logType = null, DateTime? startTime = null, DateTime? endTime = null, int page = 1, int pageSize = 10)
        {
            string query = $"/api/v1/device/{deviceId}/DeviceLog?page={page}&pageSize={pageSize}";
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
            return JsonSerializer.Deserialize<JsonArray>(content);
        }
    }
}
