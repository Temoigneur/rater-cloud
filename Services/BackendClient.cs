using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SharedModels.Evaluation;

namespace Rater.Services
{
    public class BackendClient
    {
        private readonly HttpClient _httpClient;

        public BackendClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Sends a POST request to the specified endpoint with the given payload.
        /// </summary>
        /// <typeparam name="T">The type of the payload.</typeparam>
        /// <param name="url">The endpoint URL.</param>
        /// <param name="payload">The payload to send.</param>
        /// <returns>The response content as a string.</returns>
        public async Task<string> PostAsync<T>(string url, T payload)
        {
            var jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                // Customize serializer settings if necessary
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new DefaultNamingStrategy()
                },
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Request to {url} failed with status code {response.StatusCode}: {errorContent}");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
    public class PerplexityService
    {
        private readonly HttpClient _httpClient;

        public PerplexityService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Perplexity");
        }

        public async Task<string> EvaluateAsync(EvaluationRequest request)
        {
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/perplexity/classification", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error fetching data from Perplexity API: {response.StatusCode}, Details: {errorDetails}");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
