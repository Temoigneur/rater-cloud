// File: Rater/Controllers/TrackController.cs
using Microsoft.AspNetCore.Mvc;
using Rater.Services;
using SharedModels;
using SharedModels.Track;
using SharedModels.Utilities;
using SharedModels.Response;

namespace Rater.Controllers
{
    [ApiController]
    [Route("api/track")]
    public class TrackController : ControllerBase
    {
        private readonly ISpotifyService _spotifyService;
        private readonly ILogger<TrackController> _logger;
        private readonly IApifyService _apifyService;
        private readonly ISpotifyPlayCountService _spotifyPlayCountService;
        private readonly IOpenAIService _openAIService;

        public TrackController(
            ISpotifyService spotifyService,
            ILogger<TrackController> logger,
            IApifyService apifyService,
            ISpotifyPlayCountService spotifyPlayCountService,
            IOpenAIService openAIService)
        {
            _spotifyService = spotifyService;
            _logger = logger;
            _apifyService = apifyService;
            _spotifyPlayCountService = spotifyPlayCountService;
            _openAIService = openAIService;
        }

        /// <summary>
        /// Retrieves track data based on the provided Name and optional artists.
        /// </summary>
        /// <param name="request">The track request containing track and artist names.</param>
        /// <returns>An <see cref="SharedModels.OutputResponse"/> with track details.</returns>
        [HttpPost]
        public async Task<IActionResult> GetTrackData([FromBody] TrackRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("TrackRequest validation failed: {@ModelState}", ModelState);
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Received TrackRequest: {@Request}", request);

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    _logger.LogWarning("Track name is required");
                    return BadRequest("Track name is required.");
                }

                // Ensure artistName is not null
                string artistName = request.ArtistName ?? "Unknown Artist";
                
                // Determine if this is a song navigational query
                bool isSongNavigational = IsLikelySongNavigational(request.Name, artistName);
                
                // For specific artist searches (when we have a valid artist name),
                // use a more specific search approach like we do for song navigational queries
                if (!string.IsNullOrWhiteSpace(artistName) && artistName != "Unknown Artist" && !isSongNavigational)
                {
                    _logger.LogInformation("Searching for specific track by artist: '{TrackName}' by '{ArtistName}'", request.Name, artistName);
                    
                    // Create a combined search query to help find the exact track
                    string combinedQuery = $"{request.Name} {artistName}".Trim();
                    _logger.LogInformation("Using combined search query: {Query}", combinedQuery);
                    
                    // Use the same GetTopTrackFromSpotify method we use for song navigational queries
                    // but with our combined artist+track query
                    var specificTrackData = await GetTopTrackFromSpotify(combinedQuery);
                    
                    if (specificTrackData != null)
                    {
                        _logger.LogInformation("Successfully found specific track using enhanced search: {TrackName} by {Artist} (Popularity: {Pop})",
                            specificTrackData.Name, specificTrackData.ArtistName, specificTrackData.Popularity);
                        
                        // Ensure all key properties are present
                        EnsureTrackDataCompleteness(specificTrackData, request);
                        
                        return Ok(specificTrackData);
                    }
                    
                    _logger.LogWarning("Could not find specific track with combined query, falling back to standard search");
                }
                // Handle song navigational queries
                else if (isSongNavigational)
                {
                    _logger.LogInformation("Detected song navigational query: {TrackName}", request.Name);
                    
                    // Use the enhanced track finding functionality for song navigational queries
                    var enhancedTrackData = await GetTopTrackFromSpotify(request.Name);
                    
                    if (enhancedTrackData != null)
                    {
                        _logger.LogInformation("Successfully retrieved enhanced track data: {TrackName} by {Artist} (Popularity: {Pop})",
                            enhancedTrackData.Name, enhancedTrackData.ArtistName, enhancedTrackData.Popularity);
                        
                        // Ensure all key properties are present
                        EnsureTrackDataCompleteness(enhancedTrackData, request);
                        
                        return Ok(enhancedTrackData);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve enhanced track data, falling back to standard search");
                    }
                }
                
                // If none of the special cases matched or if they failed,
                // fall back to the standard search approach
                var trackResponse = await _spotifyService.SearchTrackAsync(request.Name, artistName);

                if (trackResponse == null)
                {
                    _logger.LogWarning("Track not found: {Name} by {Artist}",
                        request.Name, artistName);
                    return NotFound(new
                    {
                        Message = "Track not found",
                        Name = request.Name,
                        Artist = artistName
                    });
                }

                // Get detailed track information
                var trackDetails = await _spotifyService.GetTrackDetailsAsync(trackResponse.Id);

                if (trackDetails == null)
                {
                    _logger.LogWarning("Track details could not be retrieved for Id: {Id}", trackResponse.Id);
                    return NotFound(new
                    {
                        Message = "Track details could not be retrieved",
                        TrackId = trackResponse.Id
                    });
                }

                // Prepare the combined response using MappingHelper
                var trackData = MappingHelper.MapToOutputResponse(trackDetails);

                // Get REAL play counts directly from SpotScraper API via SpotifyPlayCountService
                try
                {
                    // Clear cache to ensure fresh data
                    await _spotifyPlayCountService.ClearCacheAsync();
                    
                    // Use the direct SpotScraper API method from SpotifyPlayCountService
                    _logger.LogInformation("Getting REAL play count from SpotScraper API for track ID: {TrackId}", trackDetails.Id);
                    
                    // Call SpotifyPlayCountService.GetPlayCountFromSpotScraperAsync directly
                    int? realPlayCount = await _spotifyPlayCountService.GetPlayCountFromSpotScraperAsync(trackDetails.Id);
                    
                    if (realPlayCount.HasValue)
                    {
                        _logger.LogInformation("Found REAL play count from SpotScraper for track {Name}: {Count}", 
                            trackDetails.Name, realPlayCount.Value);
                            
                        // Use the REAL play count from SpotScraper
                        trackData.PlayCount = realPlayCount.Value;

                        // Calculate annual play count based on REAL data
                        double annualPlayCount = CalculateAnnualPlayCount(trackDetails, realPlayCount.Value);
                        trackData.AnnualPlayCount = (int)annualPlayCount;

                        _logger.LogInformation("Set annual play count based on REAL SpotScraper data for {TrackName}: {AnnualPlayCount}",
                            trackDetails.Name, trackData.AnnualPlayCount);
                    }
                    else
                    {
                        _logger.LogWarning("No REAL play count found from SpotScraper for track {Name}, trying fallback method", trackDetails.Name);
                        
                        // Fallback to URL-based method if ID-based method fails
                        string spotifyUrl = $"https://open.spotify.com/track/{trackDetails.Id}";
                        int? fallbackPlayCount = await _spotifyPlayCountService.GetPlayCountFromSpotScraperUrlAsync(spotifyUrl);
                        
                        if (fallbackPlayCount.HasValue)
                        {
                            _logger.LogInformation("Found REAL play count via URL from SpotScraper for track {Name}: {Count}", 
                                trackDetails.Name, fallbackPlayCount.Value);
                                
                            trackData.PlayCount = fallbackPlayCount.Value;
                            
                            // Calculate annual play count
                            double annualPlayCount = CalculateAnnualPlayCount(trackDetails, fallbackPlayCount.Value);
                            trackData.AnnualPlayCount = (int)annualPlayCount;
                        }
                        else
                        {
                            // No real play count found - do not use dummy data, just set to 0
                            _logger.LogWarning("No REAL play count data available from SpotScraper, setting to 0");
                            trackData.PlayCount = 0;
                            trackData.AnnualPlayCount = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving play counts for track {Name}", trackDetails.Name);
                    // Return 0 values
                    trackData.PlayCount = 0;
                    trackData.AnnualPlayCount = 0;
                }

                // Additional logging for debugging
                _logger.LogInformation("Track Data Retrieved: {@TrackData}", trackData);

                // Ensure all key properties are present
                EnsureTrackDataCompleteness(trackData, request);

                return Ok(trackData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comprehensive error in GetTrackData for request: {@Request}", request);
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving track data",
                    ErrorDetails = ex.Message
                });
            }
        }

        // Optional method to ensure data completeness
        private void EnsureTrackDataCompleteness(OutputResponse trackData, TrackRequest originalRequest)
        {
            // Fill in missing data from the original request if possible
            trackData.Name ??= originalRequest.Name;
            trackData.ArtistName ??= originalRequest.ArtistName;

            // Set default values for potentially missing properties
            trackData.ReleaseDate ??= "N/A";
            trackData.ReleaseDatePrecision ??= "N/A";
            trackData.Popularity ??= 0;
            trackData.PopularityRating ??= "Unknown";
        }

        /// <summary>
        /// Determines if a query is likely a song navigational query
        /// </summary>
        /// <param name="trackName">The track name</param>
        /// <param name="artistName">The artist name, if available</param>
        /// <returns>True if the query is likely a song navigational query</returns>
        private bool IsLikelySongNavigational(string trackName, string artistName)
        {
            _logger.LogInformation("Checking if query is song navigational: '{TrackName}' by '{ArtistName}'", trackName, artistName);
            
            // Special case: If artistName is empty and trackName contains "by", this is likely
            // a full navigational query like "Somewhere Out There by The Texassippi Two"
            if (string.IsNullOrWhiteSpace(artistName) || artistName.Equals("Unknown Artist", StringComparison.OrdinalIgnoreCase))
            {
                // Check if the track name contains " by " which likely indicates it's a full navigational query
                if (trackName.Contains(" by ", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Detected 'by' in the query without separate artist - treating as navigational");
                    return true;
                }
            }
            
            // If we have both track name and non-empty artist name, and artist is not "Unknown Artist",
            // it's less likely to be navigational - but could still be if manually parsed
            if (!string.IsNullOrEmpty(trackName) && !string.IsNullOrEmpty(artistName) && 
                !artistName.Equals("Unknown Artist", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            
            // Check if the track name contains common keywords that indicate it's NOT navigational
            string[] nonNavigationalKeywords = { "feat", "ft.", "remix", "cover", "live", "acoustic" };
            foreach (var keyword in nonNavigationalKeywords)
            {
                if (trackName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            // If the track name is short (1-3 words), it's more likely to be navigational
            int wordCount = trackName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount <= 3)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the top track from Spotify with enhanced play count data for song navigational queries
        /// This implements similar logic to what was in IntentController but directly
        /// </summary>
        /// <param name="query">The track name query</param>
        /// <returns>An OutputResponse with track details, or null if not found</returns>
        private async Task<OutputResponse?> GetTopTrackFromSpotify(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                _logger.LogWarning("Cannot process empty query");
                return null;
            }

            try
            {
                // Get the top 5 tracks from Spotify
                var tracks = await _spotifyService.GetTracksAsync(query, 5);
                
                if (tracks == null || !tracks.Any())
                {
                    _logger.LogWarning("No tracks found for query: {Query}", query);
                    return null;
                }
                
                // Sort tracks by popularity in descending order
                var sortedTracks = tracks.OrderByDescending(t => t.Popularity).ToList();
                
                // Log all tracks by popularity for debugging
                _logger.LogInformation("Top tracks for query '{Query}' by popularity:", query);
                foreach (var track in sortedTracks.Take(5))
                {
                    _logger.LogInformation("- {Title} by {Artist} (Popularity: {Pop})", 
                        track.Name, string.Join(", ", track.Artists.Select(a => a.Name)), track.Popularity);
                }
                
                // Take the most popular track
                var topTrack = sortedTracks.First();
                var trackDetails = await _spotifyService.GetTrackDetailsAsync(topTrack.Id);
                
                if (trackDetails == null)
                {
                    _logger.LogWarning("Could not retrieve details for top track: {TrackId}", topTrack.Id);
                    return null;
                }
                
                // Create output response
                var trackResponse = MappingHelper.MapToOutputResponse(trackDetails);
                
                // Get play count data from Apify service
                try
                {
                    // Clear cache to ensure fresh data
                    await _spotifyPlayCountService.ClearCacheAsync(trackDetails.Id);
                    
                    string spotifyUrl = $"https://open.spotify.com/track/{trackDetails.Id}";
                    var playCounts = await _apifyService.GetPlayCountsAsync(new List<string> { spotifyUrl });

                    if (playCounts != null && playCounts.Any())
                    {
                        int? playCount = null;
                        if (playCounts.TryGetValue(spotifyUrl, out playCount) && playCount.HasValue)
                        {
                            _logger.LogInformation("Found play count for top track: {Count}", playCount.Value);
                            trackResponse.PlayCount = playCount; // Assign nullable int directly
                            
                            // Calculate annual play count if release date is available
                            if (!string.IsNullOrEmpty(trackDetails.Album?.ReleaseDate))
                            {
                                double annualPlayCount = CalculateAnnualPlayCount(trackDetails, playCount.Value);
                                trackResponse.AnnualPlayCount = (int)annualPlayCount;
                            }
                        }
                    }
                    
                    // If we didn't get a play count, use popularity to estimate one
                    if (trackResponse.PlayCount == 0 && trackDetails.Popularity > 0)
                    {
                        _logger.LogInformation("No play count found, generating estimated play count from popularity: {Popularity}", trackDetails.Popularity);
                        
                        // Use popularity * some multiplier to generate a reasonable play count
                        // This is a simplistic approach - for more accuracy, use a more sophisticated model
                        int? estimatedPlayCount = trackDetails.Popularity * 100000;
                        trackResponse.PlayCount = estimatedPlayCount; // Now using nullable int
                        
                        // Calculate annual play count
                        if (!string.IsNullOrEmpty(trackDetails.Album?.ReleaseDate) && estimatedPlayCount.HasValue)
                        {
                            double annualPlayCount = CalculateAnnualPlayCount(trackDetails, estimatedPlayCount.Value);
                            trackResponse.AnnualPlayCount = (int)annualPlayCount;
                        }
                        else
                        {
                            // If no release date, use the same value
                            trackResponse.AnnualPlayCount = estimatedPlayCount;
                        }
                        
                        _logger.LogInformation("Generated estimated play count: {PlayCount}, annual: {AnnualPlayCount}", 
                            trackResponse.PlayCount, trackResponse.AnnualPlayCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving play counts for top track {Name}", trackDetails.Name);
                }
                
                return trackResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top track from Spotify for query: {Query}", query);
                return null;
            }
        }
        
        /// <summary>
        /// Calculates the annual play count based on track age
        /// </summary>
        private double CalculateAnnualPlayCount(TrackDetails track, int playCount)
        {
            // Special case: If playCount is our special marker (1000), return the marker
            if (playCount == 1000) return 1000;
            
            // If playCount is 0 or invalid, just return it as is
            if (playCount <= 0) return playCount;

            try
            {
                // Parse the release date
                DateTime releaseDate;
                if (!DateTime.TryParse(track.Album?.ReleaseDate, out releaseDate))
                {
                    // If we only have a year, use January 1st of that year
                    if (track.Album?.ReleaseDatePrecision == "year" &&
                        int.TryParse(track.Album.ReleaseDate, out int year))
                    {
                        releaseDate = new DateTime(year, 1, 1);
                    }
                    else
                    {
                        _logger.LogWarning("Could not parse release date: {ReleaseDate}", track.Album?.ReleaseDate);
                        return playCount;
                    }
                }

                // Calculate age in days
                TimeSpan age = DateTime.Now - releaseDate;
                double ageInDays = age.TotalDays;

                // For very new tracks or future releases, use a minimum of 1 day
                if (ageInDays < 1)
                    ageInDays = 1;

                // Calculate plays per day, then annualize (365.25 accounts for leap years)
                double playsPerDay = playCount / ageInDays;
                double annualPlayCount = playsPerDay * 365.25;

                _logger.LogInformation("Annual play count for {TrackName}: Age in days: {AgeInDays}, " +
                    "Plays per day: {PlaysPerDay}, Annual rate: {AnnualRate}",
                    track.Name, ageInDays, playsPerDay, annualPlayCount);

                return annualPlayCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating annual play count for track {TrackName}", track.Name);
                return playCount;
            }
        }
    }
}