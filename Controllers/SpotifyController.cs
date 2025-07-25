using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Rater.Services;
using Newtonsoft.Json;
using System.Text;
using SpotifyAPI.Web;

namespace Rater.Controllers
{
    [ApiController]
    [Route("api/spotify")]
    [EnableCors("ExternalFrontend")]
    public class SpotifyController : ControllerBase
    {
        private readonly ILogger<SpotifyController> _logger;
        private readonly ISpotifyService _spotifyService;
        private readonly IOpenAIService _openAIService;
        private readonly IApifyService _apifyService;
        private readonly ISpotifyPlayCountService _spotifyPlayCountService;

        public SpotifyController(
            ILogger<SpotifyController> logger,
            ISpotifyService spotifyService,
            IOpenAIService openAIService,
            IApifyService apifyService,
            ISpotifyPlayCountService spotifyPlayCountService)
        {
            _logger = logger;
            _spotifyService = spotifyService;
            _openAIService = openAIService;
            _apifyService = apifyService;
            _spotifyPlayCountService = spotifyPlayCountService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTracks([FromQuery] string query, [FromQuery] int limit = 8)
        {
            try
            {
                _logger.LogInformation("Searching tracks with query: {Query}, limit: {Limit}", query, limit);

                if (string.IsNullOrEmpty(query))
                {
                    return BadRequest("Search query is required");
                }

                // --- TEMPORARY TEST CODE START ---
                // Test Apify integration with a known track
                var testUrls = new List<string>
        {
            "https://open.spotify.com/track/41cpvQ2GyGb2BRdIRSsTqK" // Moon River by Frank Ocean
        };
                var testPlayCounts = await _apifyService.GetPlayCountsAsync(testUrls);
                _logger.LogInformation("TEST: Apify playcounts for Moon River: {@PlayCounts}", testPlayCounts);
                // --- TEMPORARY TEST CODE END ---

                string clarifiedQuery;
                try
                {
                    clarifiedQuery = await _openAIService.ClarifyQueryAsync(query);
                    _logger.LogInformation("Clarified query: {ClarifiedQuery} (original: {OriginalQuery})", clarifiedQuery, query);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clarify query, using original query: {Query}", query);
                    clarifiedQuery = query;
                }

                var tracks = await _spotifyService.GetTracksAsync(clarifiedQuery, limit);
                if (tracks == null || !tracks.Any())
                {
                    if (clarifiedQuery != query)
                    {
                        _logger.LogInformation("No results with clarified query, trying original query: {Query}", query);
                        tracks = await _spotifyService.GetTracksAsync(query, limit);
                    }
                    if (tracks == null || !tracks.Any())
                    {
                        return Ok(new List<object>());
                    }
                }

                // Log all items to see what types we're getting
                foreach (var item in tracks)
                {
                    _logger.LogInformation("Item: {Name}, Type: {Type}, ID: {Id}",
                        item.Name, item.Type.ToString(), item.Id);
                }

                // Filter out non-track items for play count fetching only
                var tracksOnly = tracks.Where(t => t.Type == ItemType.Track).ToList();
                _logger.LogInformation("Filtered to {Count} track items out of {TotalCount} total items",
                    tracksOnly.Count, tracks.Count);

                // Get playcounts for top 3 tracks by popularity
                var top3Tracks = tracksOnly
                    .OrderByDescending(t => t.Popularity)
                    .Take(3)
                    .ToList();

                var top3Urls = top3Tracks
                    .Select(t => $"https://open.spotify.com/track/{t.Id}")
                    .ToList();

                _logger.LogInformation("Fetching playcounts for URLs: {@Urls}", top3Urls);

                // Add a fallback for testing if Apify isn't working
                Dictionary<string, int?> playCounts;
                try
                {
                    playCounts = await _apifyService.GetPlayCountsAsync(top3Urls);

                    // If Apify returns no play counts, use special marker value (0) to indicate missing data
                    if (playCounts.Count == 0 && top3Tracks.Count > 0)
                    {
                        _logger.LogWarning("Apify returned no play counts. Using special marker value (0) to indicate missing data.");
                        playCounts = new Dictionary<string, int?>();

                        // Add special marker values for missing play counts
                        foreach (var track in top3Tracks)
                        {
                            // Use 0 for missing play counts (will display as N/A)
                            playCounts[track.Id] = 0;
                            _logger.LogInformation("Set missing play count for {TrackName} to 0", track.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching play counts. Using special marker value (0) to indicate missing data.");
                    playCounts = new Dictionary<string, int?>();

                    // Add special marker values for missing play counts
                    foreach (var track in top3Tracks)
                    {
                        // Use 0 for missing play counts (will display as N/A)
                        playCounts[track.Id] = 0;
                        _logger.LogInformation("Set missing play count for {TrackName} to 0", track.Name);
                    }
                }

                _logger.LogInformation("Final playcounts to use: {@Playcounts}", playCounts);

                // Log detailed information about top 3 tracks and their play counts
                foreach (var track in top3Tracks)
                {
                    bool hasPlayCount = playCounts.TryGetValue(track.Id, out var count);
                    _logger.LogInformation(
                        "Track: {TrackName}, ID: {TrackId}, Has play count: {HasPlayCount}, Play count: {PlayCount}",
                        track.Name, track.Id, hasPlayCount, hasPlayCount ? count : 0);
                }

                // Create response with ALL items, but only include play counts for tracks
                var response = tracks.Select(item => new
                {
                    Id = item.Id,
                    Name = item.Name,
                    Type = item.Type.ToString(),
                    Artists = item.Artists?.Select(a => new { Id = a.Id, Name = a.Name }) ??
                              new[] { new { Id = "", Name = item.Artists?.FirstOrDefault()?.Name ?? "Unknown Artist" } },
                    Album = new
                    {
                        Id = item.Album?.Id,
                        Name = item.Album?.Name,
                        ReleaseDate = item.Album?.ReleaseDate,
                        ReleaseDatePrecision = item.Album?.ReleaseDatePrecision
                    },
                    Popularity = item.Popularity,
                    playCount = (item.Type == ItemType.Track) &&
                                playCounts.TryGetValue(item.Id, out var playCount) ?
                                playCount : (int?)null,
                    PopularityRating = CategorizePopularity(item.Popularity),
                    IsExplicit = item.Explicit,
                    PreviewUrl = item.PreviewUrl,
                    ExternalUrls = item.ExternalUrls
                }).ToList();

                // Log the response being sent to the frontend
                var responseJson = JsonConvert.SerializeObject(response);
                _logger.LogInformation("Response being sent to frontend: {Response}", responseJson);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching tracks with query: {Query}", query);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private string CategorizePopularity(int? popularity)
        {
            if (!popularity.HasValue) return "Unknown";
            if (popularity >= 80) return "Very Popular";
            if (popularity >= 60) return "Popular";
            if (popularity >= 40) return "Moderately Popular";
            if (popularity >= 20) return "Below Average";
            return "Unpopular";
        }

        [HttpGet("playcount/{trackId}")]
        public async Task<IActionResult> GetPlayCount(string trackId)
        {
            try
            {
                _logger.LogInformation("Getting play count for track ID: {TrackId}", trackId);

                if (string.IsNullOrEmpty(trackId))
                {
                    return BadRequest("Track ID is required");
                }

                var playCount = await _spotifyPlayCountService.GetPlayCountFromSpotScraperAsync(trackId);
                
                if (playCount.HasValue)
                {
                    _logger.LogInformation("Successfully retrieved play count for track {TrackId}: {PlayCount}", trackId, playCount.Value);
                    return Ok(new { trackId, playCount = playCount.Value });
                }
                else
                {
                    _logger.LogWarning("No play count found for track {TrackId}", trackId);
                    return Ok(new { trackId, playCount = (int?)null });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting play count for track {TrackId}", trackId);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}