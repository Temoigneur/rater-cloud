using System.Text;
using Newtonsoft.Json;
namespace SharedModels.Services
{
    public class BackendClient
    {
        private readonly HttpClient _httpClient;
        public BackendClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<string> ProcessDataAsync(string data)
        {
            var response = await _httpClient.PostAsync("api/process", new StringContent(data, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<string> PostAsync(string endpoint, object payload)
        {
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
