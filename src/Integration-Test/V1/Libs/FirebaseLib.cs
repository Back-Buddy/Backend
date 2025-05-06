using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static BackBuddy.Integration_Test.V1.DTOs.FirebaseDto;

namespace BackBuddy.Integration_Test.V1.Libs
{
    internal class FirebaseLib
    {
        private static readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly string _projectName;

        public FirebaseLib(string baseUri, string projectName)
        {
            _httpClient = new()
            {
                BaseAddress = new Uri(baseUri),
            };
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer owner");
            _projectName = projectName;
        }

        public async Task<FirebaseRegisterResponseDto> RegisterUserAsync(string email, string password)
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"projects/{_projectName}/accounts", new
            {
                email,
                password,
                returnSecureToken = true
            });

            response.EnsureSuccessStatusCode();
            FirebaseRegisterResponseDto content = await response.Content.ReadFromJsonAsync<FirebaseRegisterResponseDto>(options: _serializerOptions);
            return content;
        }

        public async Task<FirebaseLoginResponseDto> SignInUserAsync(string email, string password)
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(GetUri("accounts:signInWithPassword"), new
            {
                email,
                password,
                returnSecureToken = true
            });
            response.EnsureSuccessStatusCode();

            FirebaseLoginResponseDto result = await response.Content.ReadFromJsonAsync<FirebaseLoginResponseDto>(options: _serializerOptions);
            return result;
        }

        public async Task<FirebaseUserQueryResponseDto> GetAllRegisteredUserAsync()
        {
            HttpResponseMessage response = await _httpClient.PostAsync(GetUri($"projects/{_projectName}/accounts:query"), new StringContent("", Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            FirebaseUserQueryResponseDto content = await response.Content.ReadFromJsonAsync<FirebaseUserQueryResponseDto>(options: _serializerOptions);
            return content;
        }

        public async Task DeleteUserAsync(string localId)
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(GetUri($"projects/{_projectName}/accounts:delete"), new
            {
                localId
            });

            response.EnsureSuccessStatusCode();
        }

        private string GetUri(string subPath) => $"{_httpClient.BaseAddress.AbsolutePath.TrimEnd('/')}/{subPath}";
    }
}
