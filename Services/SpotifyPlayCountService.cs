using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using SharedModels.Response;

namespace Rater.Services
{
    // ISpotifyPlayCountService interface
    public interface ISpotifyPlayCountService
    {
        Task<Dictionary<string, int?>> GetPlayCountsAsync(List<string> trackUrls);
        Task ClearCacheAsync(string? trackId = null);
        string? ExtractTrackIdFromUrl(string url);
        
        // New methods for direct SpotScraper API integration
        Task<int?> GetPlayCountFromSpotScraperAsync(string trackId);
        Task<int?> GetPlayCountFromSpotScraperUrlAsync(string spotifyUrl);
    }

    public class SpotifyPlayCountService : ISpotifyPlayCountService, IApifyService
    {
        private readonly ILogger<SpotifyPlayCountService> _logger;
        private readonly TimeSpan _cacheDuration;
        private static readonly Dictionary<string, CachedPlayCount> _playCountCache = new Dictionary<string, CachedPlayCount>();
        private static readonly object _lockObject = new object();
        
        // Blacklist for known incorrect play counts
        private static readonly HashSet<int> _blacklistedPlayCounts = new HashSet<int> { 2_025_000_000 };
        private static readonly Dictionary<int, int> _suspiciousPlayCounts = new Dictionary<int, int>();
        private static readonly object _suspiciousCountsLock = new object();

        // Dictionary to track play counts per track ID that should never be shared between tracks
        private static readonly Dictionary<string, List<string>> _trackPlayCounts = new Dictionary<string, List<string>>();
        private static readonly object _trackPlayCountsLock = new object();

        // This dictionary will store the last valid play count for each track ID
        private static readonly Dictionary<string, int> _validPlayCounts = new Dictionary<string, int>();
        private static readonly object _validPlayCountsLock = new object();
        
        // SpotScraper API configuration
        private readonly string _spotScraperApiKey;
        private readonly string _spotScraperBaseUrl;
        private readonly HttpClient _httpClient;

        public SpotifyPlayCountService(IConfiguration config, ILogger<SpotifyPlayCountService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _cacheDuration = TimeSpan.FromDays(7); // Cache for 7 days
            
            // Get SpotScraper API configuration from appsettings.json
            try 
            {
                _logger.LogWarning("======= INITIALIZING SPOTSCRAPER SERVICE =======");
                _logger.LogWarning("Checking configuration for SpotScraper:ApiKey and SpotScraper:BaseUrl");
                
                var apiKey = config["SpotScraper:ApiKey"];
                _logger.LogWarning("API Key exists: {ApiKeyExists}, Value: {ApiKeyPrefix}...", 
                    !string.IsNullOrEmpty(apiKey), 
                    !string.IsNullOrEmpty(apiKey) ? apiKey.Substring(0, Math.Min(5, apiKey.Length)) + "***" : "NULL");
                
                var baseUrl = config["SpotScraper:BaseUrl"];
                _logger.LogWarning("Base URL exists: {BaseUrlExists}, Value: {BaseUrl}", 
                    !string.IsNullOrEmpty(baseUrl), 
                    baseUrl ?? "Using default");
                
                _spotScraperApiKey = apiKey ?? throw new ArgumentNullException(nameof(config), "SpotScraper API key is missing from configuration");
                _spotScraperBaseUrl = baseUrl ?? "https://api.spotscraper.com/track";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL ERROR initializing SpotifyPlayCountService configuration");
                throw; // Re-throw to ensure app fails if config is missing
            }
            
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _spotScraperApiKey);
            
            _logger.LogWarning("HttpClient headers: Accept={Accept}, x-api-key={ApiKeyPresent}", 
                "application/json", !string.IsNullOrEmpty(_spotScraperApiKey));
            _logger.LogWarning("SpotifyPlayCountService initialized with SpotScraper BaseUrl: {BaseUrl}", _spotScraperBaseUrl);
        }

        // Class for caching play counts
        private class CachedPlayCount
        {
            public int PlayCount { get; set; }
            public DateTime CachedAt { get; set; }
        }

        /// <summary>
        /// Retrieves play counts for the given list of Spotify track URLs
        /// </summary>
        /// <param name="trackUrls">List of Spotify track URLs</param>
        /// <returns>Dictionary mapping track URLs to their play counts</returns>
        public async Task<Dictionary<string, int?>> GetPlayCountsAsync(List<string> trackUrls)
        {
            var result = new Dictionary<string, int?>();

            // Log starting the request and all track URLs
            _logger.LogWarning("=== SPOTSCRAPER REQUEST STARTED === Retrieving play counts for {Count} tracks", trackUrls?.Count ?? 0);
            foreach (var url in trackUrls ?? new List<string>())
            {
                _logger.LogWarning("Track URL in request: {Url}", url);
            }

            try
            {
                if (trackUrls == null || !trackUrls.Any())
                {
                    _logger.LogWarning("No track URLs provided to GetPlayCountsAsync");
                    return result;
                }

                // Process each track URL individually
                foreach (var trackUrl in trackUrls)
                {
                    _logger.LogWarning("Processing track URL: {Url}", trackUrl);
                    string? trackId = ExtractTrackIdFromUrl(trackUrl);
                    
                    if (string.IsNullOrEmpty(trackId))
                    {
                        _logger.LogWarning("Could not extract track ID from URL: {Url}", trackUrl);
                        result[trackUrl] = null;
                        continue;
                    }

                    _logger.LogWarning("Extracted track ID: {TrackId} from URL: {Url}", trackId, trackUrl);

                    // Check cache first
                    if (_playCountCache.TryGetValue(trackId, out var cachedData))
                    {
                        var age = DateTime.UtcNow - cachedData.CachedAt;
                        
                        if (age <= _cacheDuration)
                        {
                            _logger.LogWarning("Using cached play count for track {TrackId}: {PlayCount} (age: {Age} hours)",
                                trackId, cachedData.PlayCount, age.TotalHours);
                                
                            result[trackUrl] = cachedData.PlayCount;
                            continue;
                        }
                    }

                    try
                    {
                        // Get play count from SpotScraper API and always use the real data
                        _logger.LogWarning("Calling GetPlayCountFromSpotScraperAsync for track ID: {TrackId}", trackId);
                        int? playCount = await GetPlayCountFromSpotScraperAsync(trackId);
                        _logger.LogWarning("GetPlayCountFromSpotScraperAsync returned: {PlayCount}", playCount);
                        
                        if (playCount.HasValue)
                        {
                            // Even if this is a blacklisted value, still use it as the real data
                            if (_blacklistedPlayCounts.Contains(playCount.Value))
                            {
                                _logger.LogWarning("Potentially incorrect play count detected: {PlayCount} for track {TrackId}, but using it anyway", 
                                    playCount.Value, trackId);
                            }
                            
                            // Track play count occurrences for informational purposes only
                            int count;
                            lock (_suspiciousCountsLock)
                            {
                                if (_suspiciousPlayCounts.TryGetValue(playCount.Value, out count))
                                {
                                    _suspiciousPlayCounts[playCount.Value] = count + 1;
                                }
                                else
                                {
                                    _suspiciousPlayCounts[playCount.Value] = 1;
                                }
                            }
                            
                            // Log suspicious counts but still use the real value
                            if (count >= 3) // If this same value appears for 3+ tracks, it's suspicious
                            {
                                _logger.LogWarning("Potentially incorrect play count: {PlayCount} observed {Count} times, but using it anyway", 
                                    playCount.Value, count + 1);
                                    
                                // Add to blacklist for future reference (for logging purposes only)
                                lock (_suspiciousCountsLock)
                                {
                                    if (!_blacklistedPlayCounts.Contains(playCount.Value))
                                    {
                                        _blacklistedPlayCounts.Add(playCount.Value);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If SpotScraper failed, return null instead of generating an estimated count
                            _logger.LogWarning("No play count found from SpotScraper API for track {TrackId}, returning null", trackId);
                            // Note: we're no longer generating a fallback value based on popularity
                        }

                        if (playCount.HasValue)
                        {
                            // Cache the play count
                            lock (_lockObject)
                            {
                                _playCountCache[trackId] = new CachedPlayCount
                                {
                                    PlayCount = playCount.Value,
                                    CachedAt = DateTime.UtcNow
                                };
                            }
                            
                            // Store as a valid play count for this track
                            lock (_validPlayCountsLock)
                            {
                                _validPlayCounts[trackId] = playCount.Value;
                            }
                            
                            result[trackUrl] = playCount;
                            _logger.LogWarning("Successfully added play count {PlayCount} for track URL {Url}", playCount, trackUrl);
                        }
                        else
                        {
                            result[trackUrl] = null;
                            _logger.LogWarning("Added null play count for track URL {Url}", trackUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving play count for track {TrackId}", trackId);
                        result[trackUrl] = null;
                    }
                }

                _logger.LogWarning("=== FINAL RESULT === Play counts retrieved for {Count}/{Total} tracks", 
                    result.Count(kv => kv.Value.HasValue), result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving play counts");
            }

            return result;
        }

        /// <summary>
        /// Extracts the track ID from a Spotify track URL
        /// </summary>
        /// <param name="url">Spotify track URL</param>
        /// <returns>Track ID or null if extraction fails</returns>
        public string? ExtractTrackIdFromUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return null;

                _logger.LogWarning("Extracting track ID from URL: {Url}", url);

                // Extract the track ID from the URL
                // Example URL: https://open.spotify.com/track/1OxcIUqVmVYxT6427tbhDW
                var uri = new Uri(url);
                var segments = uri.Segments;

                if (segments.Length >= 3 && segments[1].Trim('/') == "track")
                {
                    string trackId = segments[2].Trim('/');
                    
                    // If there's a query string, remove it
                    int queryIndex = trackId.IndexOf('?');
                    if (queryIndex > 0)
                    {
                        trackId = trackId.Substring(0, queryIndex);
                    }
                    
                    _logger.LogWarning("Successfully extracted track ID: {TrackId}", trackId);
                    return trackId;
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

        /// <summary>
        /// Retrieves the play count for a track from the SpotScraper API
        /// </summary>
        /// <param name="trackId">Spotify track ID</param>
        /// <returns>Play count or null if retrieval fails</returns>
        public async Task<int?> GetPlayCountFromSpotScraperAsync(string trackId)
        {
            try
            {
                _logger.LogWarning("==== SPOTSCRAPER API CALL ==== Getting play count for track ID: {TrackId}", trackId);
                
                // Build the request URL for the SpotScraper API using the configured base URL
                string apiUrl = $"{_spotScraperBaseUrl}/{trackId}";
                _logger.LogWarning("Making API request to: {ApiUrl}", apiUrl);
                _logger.LogWarning("Headers: Content-Type={ContentType}, x-api-key={ApiKeyPresent}", 
                    "application/json", !string.IsNullOrEmpty(_spotScraperApiKey));
                
                // Make the request to the SpotScraper API
                var response = await _httpClient.GetAsync(apiUrl);
                
                _logger.LogWarning("API response status: {StatusCode} {IsSuccessStatusCode}", 
                    response.StatusCode, response.IsSuccessStatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("SpotScraper API error content: {ErrorContent}", errorContent);
                    return null;
                }
                
                // Parse the response JSON
                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API response content: {JsonContent}", 
                    jsonContent.Length > 200 ? jsonContent.Substring(0, 200) + "..." : jsonContent);
                
                var spotScraperResponse = JsonConvert.DeserializeObject<SpotScraperResponse>(jsonContent);
                
                _logger.LogWarning("Deserialized response: Success={Success}, HasData={HasData}, HasStatistics={HasStatistics}, HasPlayCount={HasPlayCount}",
                    spotScraperResponse?.Success,
                    spotScraperResponse?.Data != null,
                    spotScraperResponse?.Data?.Statistics != null,
                    spotScraperResponse?.Data?.Statistics?.PlayCount != null);
                
                if (spotScraperResponse?.Success == true && 
                    spotScraperResponse.Data?.Statistics?.PlayCount != null)
                {
                    // Use long (Int64) instead of int (Int32) to handle large play counts
                    // Some popular tracks like "Sweater Weather" have billions of plays
                    long playCount = spotScraperResponse.Data.Statistics.PlayCount.Value;
                    
                    // If the play count is too large for int, cap it at Int32.MaxValue
                    int safePlayCount = playCount > int.MaxValue ? int.MaxValue : (int)playCount;
                    
                    _logger.LogWarning("==== SUCCESS ==== Found play count from SpotScraper API for track {TrackId}: {PlayCount}", 
                        trackId, safePlayCount);
                    return safePlayCount;
                }
                
                _logger.LogWarning("SpotScraper API response did not contain play count data");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting play count from SpotScraper API for track {TrackId}", trackId);
                return null;
            }
        }
        
        /// <summary>
        /// Get a track's play count from SpotScraper API using a Spotify URL
        /// </summary>
        /// <param name="spotifyUrl">The full Spotify URL (e.g., https://open.spotify.com/track/1234)</param>
        /// <returns>The play count if available, or null if not available</returns>
        public async Task<int?> GetPlayCountFromSpotScraperUrlAsync(string spotifyUrl)
        {
            if (string.IsNullOrEmpty(spotifyUrl))
            {
                _logger.LogWarning("Cannot get play count without a valid Spotify URL");
                return null;
            }
            
            try
            {
                _logger.LogWarning("Getting play count from SpotScraper API for URL: {SpotifyUrl}", spotifyUrl);
                
                // Extract track ID from URL if possible
                string? trackId = null;
                if (!string.IsNullOrEmpty(spotifyUrl))
                {
                    var trackUrlMatch = Regex.Match(spotifyUrl, @"spotify\.com/track/([a-zA-Z0-9]+)");
                    if (trackUrlMatch.Success && trackUrlMatch.Groups.Count > 1)
                    {
                        trackId = trackUrlMatch.Groups[1].Value;
                        _logger.LogWarning("Extracted track ID {TrackId} from URL, trying ID-based method", trackId);
                        if (!string.IsNullOrEmpty(trackId))
                        {
                            return await GetPlayCountFromSpotScraperAsync(trackId);
                        }
                    }
                }
                
                // If we can't extract ID or URL is in a different format, use the URL directly
                _logger.LogWarning("Using direct URL query to SpotScraper API");
                
                // Construct the SpotScraper API URL with the Spotify URL parameter
                var url = $"{_spotScraperBaseUrl}?url={Uri.EscapeDataString(spotifyUrl)}";
                _logger.LogWarning("Making API request to: {Url}", url);
                
                // Use the injected HttpClient instead of creating a new one
                var response = await _httpClient.GetAsync(url);
                _logger.LogWarning("API response status: {StatusCode} {IsSuccessStatusCode}", 
                    response.StatusCode, response.IsSuccessStatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("SpotScraper API error content: {ErrorContent}", errorContent);
                    return null;
                }
                
                // Parse the response JSON
                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API response content: {JsonContent}", 
                    jsonContent.Length > 200 ? jsonContent.Substring(0, 200) + "..." : jsonContent);
                
                var spotScraperResponse = JsonConvert.DeserializeObject<SpotScraperResponse>(jsonContent);
                
                _logger.LogWarning("Deserialized response: Success={Success}, HasData={HasData}, HasStatistics={HasStatistics}, HasPlayCount={HasPlayCount}",
                    spotScraperResponse?.Success,
                    spotScraperResponse?.Data != null,
                    spotScraperResponse?.Data?.Statistics != null,
                    spotScraperResponse?.Data?.Statistics?.PlayCount != null);
                
                if (spotScraperResponse?.Success == true && 
                    spotScraperResponse.Data?.Statistics?.PlayCount != null)
                {
                    // Use long (Int64) instead of int (Int32) to handle large play counts
                    long playCount = spotScraperResponse.Data.Statistics.PlayCount.Value;
                    
                    // If the play count is too large for int, cap it at Int32.MaxValue
                    int safePlayCount = playCount > int.MaxValue ? int.MaxValue : (int)playCount;
                    
                    _logger.LogWarning("Found play count from SpotScraper API for URL {SpotifyUrl}: {PlayCount}", 
                        spotifyUrl, safePlayCount);
                    return safePlayCount;
                }
                
                _logger.LogWarning("SpotScraper API response did not contain play count data for URL");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting play count from SpotScraper API for URL {SpotifyUrl}", spotifyUrl);
                return null;
            }
        }


        /// <summary>
        /// Formats a play count number into a human-readable string with K, M, B suffixes
        /// </summary>
        /// <param name="playCount">The play count to format</param>
        /// <returns>Formatted play count string</returns>
        public static string FormatPlayCount(int? playCount)
        {
            if (!playCount.HasValue)
                return "N/A";
                
            if (playCount.Value < 0)
                return "N/A";
                
            if (playCount.Value == 0)
                return "0";
                
            if (playCount.Value < 1000)
                return playCount.Value.ToString();
                
            if (playCount.Value < 1_000_000)
                return $"{playCount.Value / 1000.0:0.#}K";
                
            if (playCount.Value < 1_000_000_000)
                return $"{playCount.Value / 1_000_000.0:0.#}M";
                
            return $"{playCount.Value / 1_000_000_000.0:0.#}B";
        }

        // Method to clear the cache for a specific track or all tracks
        public async Task ClearCacheAsync(string? trackId = null)
        {
            // Add a small delay to make this properly async
            await Task.Delay(1);
            
            if (string.IsNullOrEmpty(trackId))
            {
                _logger.LogWarning("Clearing entire play count cache with {Count} entries", _playCountCache.Count);
                lock (_lockObject)
                {
                    _playCountCache.Clear();
                }
                
                lock (_suspiciousCountsLock)
                {
                    _suspiciousPlayCounts.Clear();
                }
                
                lock (_trackPlayCountsLock)
                {
                    _trackPlayCounts.Clear();
                }
            }
            else
            {
                _logger.LogWarning("Clearing play count cache for track: {TrackId}", trackId);
                lock (_lockObject)
                {
                    _playCountCache.Remove(trackId);
                }
            }
        }
    }
}