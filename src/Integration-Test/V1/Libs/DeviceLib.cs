using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.Extensions;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Libs
{
    internal class DeviceLib
    {
        private readonly HttpClient _httpClient;

        public DeviceLib(string baseUri)
        {
            _httpClient = new()
            {
                BaseAddress = new Uri(baseUri),
            };
        }

        public async Task<JsonObject> CreateDevice(string accessToken, string deviceName)
        {
            JsonObject request = new()
            {
                { "name", deviceName }
            };
            StringContent content = new(request.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            HttpRequestMessage requestMessage = new(HttpMethod.Post, "/api/v1/device");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            requestMessage.Content = content;

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);

            string rawContent = await responseMessage.Content.ReadAsStringAsync();
            JsonObject secretObj = JsonSerializer.Deserialize<JsonObject>(rawContent);
            return secretObj;
        }

        public async Task UpdateDevice(string accessToken, Guid deviceId, string deviceName = null, TimeSpan? threshold = null, bool? active = null)
        {
            JsonObject request = [];
            if (!string.IsNullOrEmpty(deviceName) && !string.IsNullOrWhiteSpace(deviceName))
                request.Add("name", deviceName);
            if (threshold.HasValue)
                request.Add("threshold", threshold.Value.ToString());
            if (active.HasValue)
                request.Add("active", active.Value);

            StringContent content = new(request.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            HttpRequestMessage requestMessage = new(HttpMethod.Patch, $"/api/v1/device/{deviceId}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            requestMessage.Content = content;
            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);
        }

        public async Task DeleteDevice(string accessToken, Guid deviceId)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Delete, $"/api/v1/device/{deviceId}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);
        }

        public async Task<JsonObject> GetDevice(string accessToken, Guid deviceId)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, $"/api/v1/device/{deviceId}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);

            string rawContent = await responseMessage.Content.ReadAsStringAsync();
            JsonObject secretObj = JsonSerializer.Deserialize<JsonObject>(rawContent);
            return secretObj;
        }

        public async Task<(JsonArray, bool)> GetDevices(string accessToken, int page = 1, int size = 10, bool? active = null, bool? descending = null)
        {
            string query = $"/api/v1/device?page={page}&size={size}";
            
            if (active.HasValue)
                query += $"&active={active.Value}";

            if (descending.HasValue)
                query += $"&descending={descending.Value}";
            
            HttpRequestMessage requestMessage = new(HttpMethod.Get, query);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
                throw new RequestFailedException(responseMessage);

            string rawContent = await responseMessage.Content.ReadAsStringAsync();
            JsonArray devices = JsonSerializer.Deserialize<JsonArray>(rawContent);

            bool hasMoreEntries = bool.Parse(responseMessage.Headers.GetValues("X-Has-More-Entries").First().ToString());
            return (devices, hasMoreEntries);
        }

        public async Task<Guid> CreateSimpleDevice(string accessToken, string name)
        {
            JsonObject secretObj = await CreateDevice(accessToken, name);
            Guid deviceId = Guid.Parse(secretObj["deviceId"].AsValue().GetValue<string>());
            return deviceId;
        }

        public static JsonObject CreateUpdateStatus(string status)
        {
            JsonObject request = new()
            {
                { "MessageType", "DeviceUpdateStatus" },
                { "UserPositionStatus", status }
            };
            return request;
        }
    }
}
