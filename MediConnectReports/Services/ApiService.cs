using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MediConnectReports.Models;

namespace MediConnectReports.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string?> LoginAsync(LoginViewModel model)
        {
            var client = _httpClientFactory.CreateClient("MediConnectAPI");

            var json = JsonSerializer.Serialize(new
            {
                userName = model.UserName,
                password = model.Password
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/auth/login", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseJson = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(responseJson);

            return document.RootElement.GetProperty("token").GetString();
        }

        public async Task<T?> GetAsync<T>(string endpoint, string token)
        {
            var client = _httpClientFactory.CreateClient("MediConnectAPI");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
                return default;

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}