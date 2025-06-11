using BackBuddy.Integration_Test.Exceptions;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BackBuddy.Integration_Test.V1.Libs
{
    internal class UserLib
    {
        private readonly HttpClient _httpClient;

        public UserLib(string baseUri)
        {
            _httpClient = new()
            {
                BaseAddress = new Uri(baseUri)
            };
        }

        public async Task DeleteUser(string accessToken)
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Delete, "api/v1/user");
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new RequestFailedException(response);
            }
        }

        public async Task<JsonArray> SearchUser(string accessToken, string searchTerm, int limit = 10, string expandType = "None")
        {
            string url = $"api/v1/user/search?searchTerm={Uri.EscapeDataString(searchTerm)}&limit={limit}&expandType={expandType}";
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, url);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);

            if (!response.IsSuccessStatusCode)
            {
                throw new RequestFailedException(response);
            }
            string content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonArray>(content);
        }

        public async Task<JsonObject> GetUser(string accessToken, string userId, string expandType = "None")
        {
            string url = $"api/v1/user/{userId}?expandType={expandType}";
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, url);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new RequestFailedException(response);
            }
            string content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonObject>(content);
        }

        public async Task FollowUser(string accessToken, string userId)
        {
            string url = $"api/v1/user/{userId}/follow";
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Put, url);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new RequestFailedException(response);
            }
        }

        public async Task UnfollowUser(string accessToken, string userId)
        {
            string url = $"api/v1/user/{userId}/follow";
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Delete, url);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new RequestFailedException(response);
            }
        }

        public async Task<(JsonArray Followers, bool HasMoreEntries)> GetFollowers(string accessToken, string userId, string expandType = "None", int page = 1, int size = 10)
        {
            string url = $"api/v1/user/{userId}/followers?expandType={expandType}&page={page}&size={size}";

            HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, url);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new RequestFailedException(response);
            }
            string content = await response.Content.ReadAsStringAsync();
            bool hasMoreEntries = bool.Parse(response.Headers.GetValues("X-Has-More-Entries").First().ToString());
            return (JsonSerializer.Deserialize<JsonArray>(content), hasMoreEntries);
        }

        public async Task<(JsonArray Following, bool HasMoreEntries)> GetFollowing(string accessToken, string userId, string expandType = "None", int page = 1, int size = 10)
        {
            string url = $"api/v1/user/{userId}/following?expandType={expandType}&page={page}&size={size}";
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, url);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new RequestFailedException(response);
            }
            string content = await response.Content.ReadAsStringAsync();
            bool hasMoreEntries = bool.Parse(response.Headers.GetValues("X-Has-More-Entries").First().ToString());
            return (JsonSerializer.Deserialize<JsonArray>(content), hasMoreEntries);
        }
    }
}