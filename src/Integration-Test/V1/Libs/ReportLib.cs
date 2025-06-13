using BackBuddy.Integration_Test.Exceptions;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Libs
{
    internal class ReportLib
    {

        private readonly HttpClient _httpClient;

        public ReportLib(string baseUri)
        {
            _httpClient = new()
            {
                BaseAddress = new Uri(baseUri),
            };
        }

        public async Task<JsonObject> CreateReport(string accessToken, Guid deviceId, string name, string visibilityType, DateTime startTime, DateTime endTime)
        {
            JsonObject request = new()
            {
                ["name"] = name,
                ["visibilityType"] = visibilityType,
                ["deviceId"] = deviceId,
                ["startTime"] = startTime,
                ["endTime"] = endTime
            };
            StringContent content = new(request.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            HttpRequestMessage requestMessage = new(HttpMethod.Post, "/api/v1/report");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            requestMessage.Content = content;

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);

            string rawContent = await responseMessage.Content.ReadAsStringAsync();
            JsonObject reportObj = JsonSerializer.Deserialize<JsonObject>(rawContent);
            return reportObj;
        }

        public async Task UpdateReport(string accessToken, Guid reportId, string name = null, string visibilityType = null)
        {
            JsonObject request = [];
            if (name != null)
                request["name"] = name;
            if (visibilityType != null)
                request["visibilityType"] = visibilityType;

            StringContent content = new(request.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            HttpRequestMessage requestMessage = new(HttpMethod.Patch, $"/api/v1/report/{reportId}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            requestMessage.Content = content;

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);
        }

        public async Task<JsonObject> GetReport(string accessToken, Guid reportId, string expandType = "None")
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, $"/api/v1/report/{reportId}?expandType={expandType}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);

            string rawContent = await responseMessage.Content.ReadAsStringAsync();
            JsonObject reportObj = JsonSerializer.Deserialize<JsonObject>(rawContent);
            return reportObj;
        }

        public async Task<(JsonArray reports, bool hasMoreEntries)> GetReports(string accessToken, List<Guid> deviceIds = null, DateTime? startTime = null, DateTime? endTime = null, bool descending = true, int pageSize = 10, int page = 1, string expandType = "None")
        {
            StringBuilder queryBuilder = new();
            queryBuilder.Append($"?size={pageSize}&page={page}&expandType={expandType}");

            foreach (Guid deviceId in deviceIds ?? [])
            {
                queryBuilder.Append($"&Devices={deviceId}");
            }
            if (startTime.HasValue)
                queryBuilder.Append($"&startTime={startTime.Value:o}");
            if (endTime.HasValue)
                queryBuilder.Append($"&endTime={endTime.Value:o}");
            queryBuilder.Append($"&descending={descending}");

            HttpRequestMessage requestMessage = new(HttpMethod.Get, $"/api/v1/report{queryBuilder}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);

            string rawContent = await responseMessage.Content.ReadAsStringAsync();
            JsonArray reports = JsonSerializer.Deserialize<JsonArray>(rawContent);

            bool hasMoreEntries = bool.Parse(responseMessage.Headers.GetValues("X-Has-More-Entries").First().ToString());
            return (reports, hasMoreEntries);
        }

        public async Task DeleteReport(string accessToken, Guid reportId)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Delete, $"/api/v1/report/{reportId}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);
        }

        public async Task CreateSampleReports(string websocketUri, string deviceSecret, string accessToken, Guid deviceId, string name, string visibilityType, int count, TimeSpan delay)
        {
            for (int i = 0; i < count; i++)
            {
                DateTime start = DateTime.UtcNow;
                await DeviceLogLib.CreateSampleLogs(websocketUri, deviceSecret, 1, 0, delay);
                await CreateReport(accessToken, deviceId, name, visibilityType, start, DateTime.UtcNow);
            }
        }

    }
}
