// File: Rater/Controllers/OutputController.cs
using Microsoft.AspNetCore.Mvc;
using Rater.Services;
using SharedModels.Request;
using SharedModels.Utilities;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Rater.Controllers
{
    [ApiController]
    [Route("api/output")]
    public class OutputController : ControllerBase
    {
        private readonly ISpotifyService _spotifyService;
        private readonly ILogger<OutputController> _logger;
        private readonly IApifyService _apifyService;
        private readonly IConfiguration _configuration;
        private readonly ISpotifyPlayCountService _spotifyPlayCountService;

        public OutputController(
            ISpotifyService spotifyService,
            ILogger<OutputController> logger,
            IApifyService apifyService,
            IConfiguration configuration,
            ISpotifyPlayCountService spotifyPlayCountService)
        {
            _spotifyService = spotifyService;
            _logger = logger;
            _apifyService = apifyService;
            _configuration = configuration;
            _spotifyPlayCountService = spotifyPlayCountService;
        }

        /// <summary>
        /// Receives an OutputRequest and processes it to retrieve album or track details.
        /// </summary>
        /// <param name="request">The OutputRequest containing OutputType and relevant IDs.</param>
        /// <returns>An <see cref="OutputResponse"/> with detailed information.</returns>
        [HttpPost]
        public async Task<IActionResult> ReceiveOutput([FromBody] OutputRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("OutputRequest validation failed: {@ModelState}", ModelState);
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Received OutputRequest: {@Request}", request);

                if (request.OutputType.Equals("Album", StringComparison.OrdinalIgnoreCase))
                {
                    // Allow either ID or Name+Artist combination
                    if (string.IsNullOrWhiteSpace(request.Id) && 
                        (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ArtistName)))
                    {
                        _logger.LogWarning("Either Id or both Name and ArtistName are required when OutputType is 'Album'.");
                        return BadRequest("Either Id or both Name and ArtistName are required when OutputType is 'Album'.");
                    }

                    // Process Album
                    return await ProcessAlbumRequest(request);
                }
                else if (request.OutputType.Equals("Track", StringComparison.OrdinalIgnoreCase))
                {
                    // Allow either ID or Name+Artist combination
                    if (string.IsNullOrWhiteSpace(request.Id) && 
                        (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ArtistName)))
                    {
                        _logger.LogWarning("Either Id or both Name and ArtistName are required when OutputType is 'Track'.");
                        return BadRequest("Either Id or both Name and ArtistName are required when OutputType is 'Track'.");
                    }

                    // Process Track
                    return await ProcessTrackRequest(request);
                }
                else
                {
                    _logger.LogWarning("Unsupported OutputType received: {OutputType}", request.OutputType);
                    return BadRequest("Unsupported OutputType. Allowed values are 'Album' or 'Track'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReceiveOutput");
                return StatusCode(500, new { message = "An error occurred while processing the output." });
            }
        }

        /// <summary>
        /// Processes an album request by retrieving album details.
        /// </summary>
        /// <param name="request">The OutputRequest containing Id or Name and ArtistName.</param>
        /// <returns>An <see cref="OutputResponse"/> with album details.</returns>
        private async Task<IActionResult> ProcessAlbumRequest(OutputRequest request)
        {
            try
            {
                _logger.LogInformation("Processing album request: {@Request}", request);
                
                string albumId = request.Id;
                
                // If no ID is provided, but name and artist are, search for the album ID first
                if (string.IsNullOrEmpty(albumId) && !string.IsNullOrEmpty(request.Name) && !string.IsNullOrEmpty(request.ArtistName))
                {
                    _logger.LogWarning("*** DEBUG: Request values - Name: {Name}, Artist: {Artist} ***",
                        request.Name ?? "NULL", request.ArtistName ?? "NULL");
                    _logger.LogInformation("Searching for album: \"{Name}\" by \"{Artist}\"", request.Name, request.ArtistName);

                    // Search for the album using name and artist to get a valid Spotify album ID
                    _logger.LogWarning("*** DEBUG: About to call SearchAlbumAsync ***");
                    var albumResponse = await _spotifyService.SearchAlbumAsync(request.Name, request.ArtistName);
                    _logger.LogWarning("*** DEBUG: SearchAlbumAsync result: {Result} ***",
                        albumResponse == null ? "NULL" : $"ID: {albumResponse.Id}, Name: {albumResponse.Name}");

                    if (albumResponse == null || string.IsNullOrEmpty(albumResponse.Id))
                    {
                        _logger.LogWarning("Album not found: \"{Name}\" by \"{Artist}\"", request.Name, request.ArtistName);
                        return BadRequest($"Album not found: \"{request.Name}\" by \"{request.ArtistName}\"");
                    }
                    
                    _logger.LogInformation("Found album: {Id} - \"{Name}\" by \"{Artist}\"", 
                        albumResponse.Id, albumResponse.Name, albumResponse.ArtistName);
                    
                    albumId = albumResponse.Id;
                }

                // Retrieve album details from Spotify API
                var albumDetails = await _spotifyService.GetAlbumDetailsAsync(albumId);

                if (albumDetails == null)
                {
                    _logger.LogWarning("Album details could not be retrieved for Id: {Id}", albumId);
                    return BadRequest("Album details could not be retrieved.");
                }

                // Prepare response using MappingHelper
                var response = MappingHelper.MapToOutputResponse(albumDetails);

                _logger.LogInformation("Response prepared for Id: {Id}", albumId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing album request");
                return StatusCode(500, new { message = "An error occurred while processing the album request." });
            }
        }

        /// <summary>
        /// Processes a track request by retrieving track details.
        /// </summary>
        /// <param name="request">The OutputRequest containing Name and ArtistName.</param>
        /// <returns>An <see cref="OutputResponse"/> with track details.</returns>
        private async Task<IActionResult> ProcessTrackRequest(OutputRequest request)
        {
            try
            {
                _logger.LogInformation("Processing track request: {@Request}", request);
                
                // Check if we have name and artist to search for the track - this is the primary method we should use
                if (!string.IsNullOrEmpty(request.Name) && !string.IsNullOrEmpty(request.ArtistName))
                {
                    _logger.LogWarning("*** DEBUG: Request values - Name: {Name}, Artist: {Artist} ***",
                        request.Name ?? "NULL", request.ArtistName ?? "NULL");
                    _logger.LogInformation("Searching for track: \"{Name}\" by \"{Artist}\"", request.Name, request.ArtistName);


                    // Search for the track using name and artist to get a valid Spotify track ID
                    _logger.LogWarning("*** DEBUG: About to call SearchTrackAsync ***");
                    var trackResponse = await _spotifyService.SearchTrackAsync(request.Name, request.ArtistName);
                    _logger.LogWarning("*** DEBUG: SearchTrackAsync result: {Result} ***",
    trackResponse == null ? "NULL" : $"ID: {trackResponse.Id}, Name: {trackResponse.Name}");

                    if (trackResponse == null || string.IsNullOrEmpty(trackResponse.Id))
                    {
                        _logger.LogWarning("Track not found: \"{Name}\" by \"{Artist}\"", request.Name, request.ArtistName);
                        return BadRequest($"Track not found: \"{request.Name}\" by \"{request.ArtistName}\"");
                    }
                    
                    _logger.LogInformation("Found track: {Id} - \"{Name}\" by \"{Artist}\"", 
                        trackResponse.Id, trackResponse.Name, trackResponse.ArtistName);
                    
                    // Now that we have a valid Spotify track ID, get detailed track information
                    var trackDetails = await _spotifyService.GetTrackDetailsAsync(trackResponse.Id);
                    
                    if (trackDetails == null)
                    {
                        _logger.LogWarning("Track details could not be retrieved for Id: {Id}", trackResponse.Id);
                        return BadRequest("Track details could not be retrieved.");
                    }
                    
                    // Get the play count using the Spotify track ID
                    string trackUrl = $"https://open.spotify.com/track/{trackResponse.Id}";
                    var playCount = await _spotifyPlayCountService.GetPlayCountFromSpotScraperUrlAsync(trackUrl);
                    
                    if (playCount.HasValue)
                    {
                        _logger.LogInformation("Retrieved play count for track {Id} - \"{Name}\" by \"{Artist}\": {PlayCount}", 
                            trackResponse.Id, trackResponse.Name, trackResponse.ArtistName, playCount.Value);
                        
                        trackDetails.PlayCount = playCount.Value;
                        
                        // Calculate annual play count if we have release date information
                        if (!string.IsNullOrEmpty(trackDetails.ReleaseDate))
                        {
                            double totalDays = 365.25; // Default to 1 year if we can't parse date
                            
                            DateTime parsedReleaseDate;
                            if (DateTime.TryParse(trackDetails.ReleaseDate, out parsedReleaseDate))
                            {
                                totalDays = (DateTime.UtcNow - parsedReleaseDate).TotalDays;
                                if (totalDays <= 0) totalDays = 1; // Avoid division by zero
                            }
                            
                            // Calculate annual play count
                            double playsPerDay = playCount.Value / totalDays;
                            trackDetails.AnnualPlayCount = (int)Math.Round(playsPerDay * 365.25);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not retrieve play count for track {Id} - \"{Name}\" by \"{Artist}\"", 
                            trackResponse.Id, trackResponse.Name, trackResponse.ArtistName);
                    }
                    
                    // Prepare response using MappingHelper
                    var response = MappingHelper.MapToOutputResponse(trackDetails);
                    return Ok(response);
                }
                // Fall back to ID-based lookup if someone provides a proper Spotify ID but no name/artist
                // (unlikely given the search input format you described, but included for completeness)
                else if (!string.IsNullOrEmpty(request.Id))
                {
                    _logger.LogWarning("Only ID provided without Name and Artist - this is not recommended. ID: {Id}", request.Id);
                    
                    // Try using the ID directly (only if it looks like a valid Spotify ID)
                    if (request.Id.Length >= 10 && !request.Id.Contains(" "))
                    {
                        // Retrieve track details from Spotify API
                        var trackDetails = await _spotifyService.GetTrackDetailsAsync(request.Id);

                        if (trackDetails == null)
                        {
                            _logger.LogWarning("Track details could not be retrieved for Id: {Id}", request.Id);
                            return BadRequest("Track details could not be retrieved using the provided ID. Please provide Name and Artist instead.");
                        }

                        // Get the play count using the track ID
                        string trackUrl = $"https://open.spotify.com/track/{request.Id}";
                        var playCount = await _spotifyPlayCountService.GetPlayCountFromSpotScraperUrlAsync(trackUrl);
                        
                        if (playCount.HasValue)
                        {
                            trackDetails.PlayCount = playCount.Value;
                            
                            // Calculate annual play count if we have release date information
                            if (!string.IsNullOrEmpty(trackDetails.ReleaseDate))
                            {
                                double totalDays = 365.25; // Default to 1 year if we can't parse date
                                
                                DateTime parsedReleaseDate;
                                if (DateTime.TryParse(trackDetails.ReleaseDate, out parsedReleaseDate))
                                {
                                    totalDays = (DateTime.UtcNow - parsedReleaseDate).TotalDays;
                                    if (totalDays <= 0) totalDays = 1; // Avoid division by zero
                                }
                                
                                // Calculate annual play count
                                double playsPerDay = playCount.Value / totalDays;
                                trackDetails.AnnualPlayCount = (int)Math.Round(playsPerDay * 365.25);
                            }
                        }

                        // Prepare response using MappingHelper
                        var response = MappingHelper.MapToOutputResponse(trackDetails);
                        return Ok(response);
                    }
                    else
                    {
                        return BadRequest("The provided ID is not a valid Spotify track ID. Please provide Name and Artist instead.");
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid request: Name and ArtistName must be provided");
                    return BadRequest("Name and ArtistName must be provided to search for a track.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing track request");
                return StatusCode(500, new { message = "An error occurred while processing the track request." });
            }
        }
    }
}
