using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedModels.Response;

namespace Rater.Services
{
    public class ApifyService : IApifyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApifyService> _logger;
        private readonly IConfiguration _config;

        // Key rotation variables
        private static int _currentKeyIndex = 0;
        private static readonly object _lockObject = new object();
        private static readonly Dictionary<string, ApiKeyUsage> _keyUsage = new Dictionary<string, ApiKeyUsage>();
        private static readonly TimeSpan _resetPeriod = TimeSpan.FromHours(24); // Reset usage count after 24 hours

        // Cache for play counts to reduce API calls
        private static readonly Dictionary<string, CachedPlayCount> _playCountCache = new Dictionary<string, CachedPlayCount>();
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromDays(7); // Cache for 7 days

        public ApifyService(HttpClient httpClient, IConfiguration config, ILogger<ApifyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
        }

        // Class to track API key usage
        private class ApiKeyUsage
        {
            public string Key { get; set; }
            public int RequestCount { get; set; }
            public DateTime LastUsed { get; set; }
        }

        // Class for caching play counts
        private class CachedPlayCount
        {
            public int PlayCount { get; set; }
            public DateTime CachedAt { get; set; }
        }

        // Get the next API key in rotation
        private string GetNextApiKey()
        {
            lock (_lockObject)
            {
                var apiKeys = _config.GetSection("Apify:ApiTokens").Get<string[]>() ??
                              new[] { _config["Apify:ApiToken"] }; // Fallback to single key if array not found

                if (apiKeys.Length == 0)
                {
                    _logger.LogWarning("No Apify API keys configured!");
                    return string.Empty;
                }

                var now = DateTime.UtcNow;

                // Try to find a key that hasn't been used too much recently
                foreach (var key in apiKeys)
                {
                    if (string.IsNullOrEmpty(key)) continue;

                    if (!_keyUsage.ContainsKey(key))
                    {
                        _keyUsage[key] = new ApiKeyUsage { Key = key, RequestCount = 1, LastUsed = now };
                        _logger.LogInformation("Using new API key: {KeyPrefix}...", MaskKey(key));
                        return key;
                    }

                    var usage = _keyUsage[key];

                    // If the key hasn't been used in the reset period, reset its count
                    if ((now - usage.LastUsed) >= _resetPeriod)
                    {
                        _logger.LogInformation("Resetting usage count for API key: {KeyPrefix}...", MaskKey(key));
                        usage.RequestCount = 1;
                        usage.LastUsed = now;
                        return key;
                    }

                    // If the key has been used less than the limit (10 requests per day is a safe estimate for free tier)
                    if (usage.RequestCount < 10)
                    {
                        usage.RequestCount++;
                        usage.LastUsed = now;
                        _logger.LogInformation("Using API key: {KeyPrefix}... (Usage: {Count}/10)", MaskKey(key), usage.RequestCount);
                        return key;
                    }
                }

                // If all keys are at their limit, use the least recently used one
                if (_keyUsage.Count > 0)
                {
                    var leastRecentlyUsed = _keyUsage.OrderBy(kv => kv.Value.LastUsed).First();
                    leastRecentlyUsed.Value.RequestCount++;
                    leastRecentlyUsed.Value.LastUsed = now;
                    _logger.LogWarning("All API keys at limit. Using least recently used key: {KeyPrefix}...",
                        MaskKey(leastRecentlyUsed.Key));
                    return leastRecentlyUsed.Key;
                }

                // Fallback to first key if something went wrong
                _logger.LogWarning("Key usage tracking issue. Falling back to first key.");
                return apiKeys[0];
            }
        }

        // Mask API key for logging (show only first 5 chars)
        private string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "empty";
            if (key.Length <= 5) return key;
            return key.Substring(0, 5) + "...";
        }

        public async Task<Dictionary<string, int?>> GetPlayCountsAsync(List<string> trackUrls)
        {
            var result = new Dictionary<string, int?>();

            // Log starting the Apify request and all track URLs
            _logger.LogInformation("=== APIFY REQUEST STARTED === Retrieving play counts for {Count} tracks", trackUrls?.Count ?? 0);
            foreach (var url in trackUrls ?? new List<string>())
            {
                _logger.LogInformation("Track URL in request: {Url}", url);
            }

            try
            {
                if (trackUrls == null || !trackUrls.Any())
                {
                    _logger.LogWarning("No track URLs provided to GetPlayCountsAsync");
                    return result;
                }

                // Check cache first
                foreach (var url in trackUrls)
                {
                    string trackId = ExtractTrackIdFromUrl(url);
                    
                    if (!string.IsNullOrEmpty(trackId) && _playCountCache.TryGetValue(trackId, out var cachedData))
                    {
                        var age = DateTime.UtcNow - cachedData.CachedAt;
                        
                        if (age <= _cacheDuration)
                        {
                            _logger.LogInformation("Using cached play count for track {TrackId}: {PlayCount} (age: {Age} hours)",
                                trackId, cachedData.PlayCount, age.TotalHours);
                            
                            result[url] = cachedData.PlayCount;
                            result[trackId] = cachedData.PlayCount;
                        }
                        else
                        {
                            _logger.LogInformation("Cache expired for track {TrackId} (age: {Age} hours)",
                                trackId, age.TotalHours);
                        }
                    }
                }

                // If all URLs were served from cache, return early
                if (result.Count >= trackUrls.Count)
                {
                    _logger.LogInformation("All {Count} track play counts served from cache", result.Count);
                    return result;
                }

                // Get API token
                string apiToken = GetNextApiKey();
                if (string.IsNullOrEmpty(apiToken))
                {
                    _logger.LogError("No Apify API token available");
                    return result;
                }

                var requestBody = new
                {
                    urls = trackUrls
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                // Log the exact request being sent
                _logger.LogInformation("Sending request to Apify with URLs: {Urls}", JsonConvert.SerializeObject(trackUrls));

                // Add the API key to the request
                var requestUrl = "https://api.apify.com/v2/acts/beatanalytics~spotify-play-count-scraper/run-sync-get-dataset-items?token=" + apiToken;

                var response = await _httpClient.PostAsync(requestUrl, content);

                // Handle rate limiting or unauthorized errors
                int retryCount = 0;
                const int maxRetries = 3;

                while ((response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                        response.StatusCode == System.Net.HttpStatusCode.Unauthorized) &&
                       retryCount < maxRetries)
                {
                    _logger.LogWarning("API limit reached or unauthorized with key {KeyPrefix}. Trying next key...",
                        MaskKey(apiToken));

                    // Try with the next key
                    apiToken = GetNextApiKey();
                    if (string.IsNullOrEmpty(apiToken))
                    {
                        _logger.LogError("No more API keys available after retry");
                        break;
                    }

                    _logger.LogInformation("Retrying with Apify API key: {KeyPrefix}...", MaskKey(apiToken));

                    requestUrl = "https://api.apify.com/v2/acts/beatanalytics~spotify-play-count-scraper/run-sync-get-dataset-items?token=" + apiToken;
                    response = await _httpClient.PostAsync(requestUrl, content);

                    retryCount++;
                }

                // If we still have an error after retries, return what we have from cache
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API authentication failed. Tokens may need to be refreshed.");
                    return result; // Return what we have from cache without retrying
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Raw Apify response: {Response}", responseContent);

                // Add detailed logging about the response
                if (string.IsNullOrEmpty(responseContent) || responseContent == "[]")
                {
                    _logger.LogWarning("Apify returned empty response. API keys may need refreshing or the service endpoint may have changed.");
                    return result;
                }

                try
                {
                    var apiResults = JsonConvert.DeserializeObject<List<ApifyResult>>(responseContent);

                    if (apiResults == null || !apiResults.Any())
                    {
                        _logger.LogWarning("Apify returned no results");
                        return result; // Return what we have from cache
                    }

                    _logger.LogInformation("Successfully parsed {Count} results from Apify", apiResults.Count);

                    foreach (var apiResult in apiResults)
                    {
                        _logger.LogInformation("Processing Apify result: URL={Url}, PlayCount={PlayCount}",
                            apiResult.url, apiResult.playCount);

                        // Extract track ID from URL
                        var trackId = ExtractTrackIdFromUrl(apiResult.url);
                        if (!string.IsNullOrEmpty(trackId) && apiResult.playCount.HasValue)
                        {
                            // Store by both URL and ID for more reliable lookups
                            result[apiResult.url] = apiResult.playCount.Value;
                            result[trackId] = apiResult.playCount.Value;
                            _playCountCache[trackId] = new CachedPlayCount
                            {
                                PlayCount = apiResult.playCount.Value,
                                CachedAt = DateTime.UtcNow
                            };
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error parsing Apify response: {Response}", responseContent);
                    return result; // Return what we have from cache
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting play counts from Apify");
                return result; // Return what we have from cache
            }
        }

        // Method to clear the cache for a specific track or all tracks
        public async Task ClearCacheAsync(string? trackId = null)
        {
            // Add a small delay to make this properly async
            await Task.Delay(1);
            
            if (string.IsNullOrEmpty(trackId))
            {
                _logger.LogInformation("Clearing entire play count cache containing {Count} items", _playCountCache.Count);
                lock (_lockObject)
                {
                    _playCountCache.Clear();
                }
            }
            else if (_playCountCache.ContainsKey(trackId))
            {
                _logger.LogInformation("Clearing Apify cache entry for track {TrackId}", trackId);
                lock (_lockObject)
                {
                    _playCountCache.Remove(trackId);
                }
            }
        }

        public string? ExtractTrackIdFromUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return null;

                // Extract the track ID from the URL
                // Example URL: https://open.spotify.com/track/1OxcIUqVmVYxT6427tbhDW
                var uri = new Uri(url);
                var segments = uri.Segments;

                if (segments.Length >= 3 && segments[1].Trim('/') == "track")
                {
                    return segments[2].Trim('/');
                }

                _logger.LogWarning("Could not extract track ID from URL: {Url}", url);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting track ID from URL: {Url}", url);
                return null;
            }
        }

        public class ApifyResult
        {
            public string url { get; set; }
            public int? playCount { get; set; }
        }
    }
}