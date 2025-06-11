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

        public async Task<JsonArray> SearchUser(string accessToken, string searchTerm, int limit = 10)
        {
            string url = $"api/v1/user/search?searchTerm={Uri.EscapeDataString(searchTerm)}&limit={limit}";
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
    }
}
