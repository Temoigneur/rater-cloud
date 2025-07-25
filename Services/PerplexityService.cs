using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Rater.Services
{
    public class RaterPerplexityService : IPerplexityService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _perplexityApiKey;
        private readonly string _perplexityBaseUrl;
        private readonly ILogger<RaterPerplexityService> _logger;

        public RaterPerplexityService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<RaterPerplexityService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;

            _perplexityApiKey = configuration["Perplexity:ApiKey"];
            _perplexityBaseUrl = "https://api.perplexity.ai/chat/completions";

            if (string.IsNullOrEmpty(_perplexityApiKey))
            {
                _logger.LogWarning("Perplexity API key not configured. Using default fallback.");
            }
        }

        public async Task<string> GetAIResponseAsync(string prompt)
        {
            try
            {
                if (string.IsNullOrEmpty(_perplexityApiKey))
                {
                    _logger.LogWarning("Perplexity API key not configured. Returning empty response.");
                    return string.Empty;
                }

                var requestData = new
                {
                    model = "llama-3-sonar-large-32k-online",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful AI assistant specialized in music information." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.2,
                    max_tokens = 300
                };

                var requestContent = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _perplexityApiKey);

                var response = await _httpClient.PostAsync(_perplexityBaseUrl, requestContent);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<PerplexityResponse>(jsonResponse);

                if (responseObj?.choices?.Length > 0 && 
                    responseObj.choices[0]?.message?.content != null)
                {
                    return responseObj.choices[0].message.content;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Perplexity API: {Message}", ex.Message);
                return string.Empty;
            }
        }

        private class PerplexityResponse
        {
            public Choice[]? choices { get; set; }

            public class Choice
            {
                public Message? message { get; set; }
            }

            public class Message
            {
                public string? content { get; set; }
            }
        }
    }
}
