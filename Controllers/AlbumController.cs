using Microsoft.AspNetCore.Mvc;
using Rater.Services;
using SharedModels.Album;
using SharedModels.Request;
using SharedModels.Track;
using SharedModels.Utilities;
using SpotifyAPI.Web;

namespace Rater.Controllers
{
    [ApiController]
    [Route("api/album")]
    public class AlbumController : ControllerBase
    {
        private readonly ILogger<AlbumController> _logger;
        private readonly ISpotifyService _spotifyService;

        public AlbumController(ILogger<AlbumController> logger, ISpotifyService spotifyService)
        {
            _logger = logger;
            _spotifyService = spotifyService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchAlbum(string query)
        {
            try
            {
                var albums = await _spotifyService.SearchAlbumAsync(query);
                if (albums == null || albums.Count == 0)
                {
                    _logger.LogWarning("No albums found for query: {Query}", query);
                    return NotFound("No albums found.");
                }

                var firstAlbum = albums[0];
                var albumDetails = await _spotifyService.GetAlbumDetailsAsync(firstAlbum.Id);

                var response = MappingHelper.MapToOutputResponse(albumDetails);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching for album");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("output")]
        public async Task<IActionResult> ReceiveOutput(OutputResponse request)
        {
            _logger.LogInformation("Received output request: {@Request}", request);

            // Check both lowercase and uppercase output types for compatibility
            if (request.OutputType.Equals("album", StringComparison.OrdinalIgnoreCase))
            {
                // Use either AlbumID or Id, whichever is available
                string albumId = !string.IsNullOrEmpty(request.AlbumID) ? request.AlbumID : request.Id;

                if (string.IsNullOrEmpty(albumId))
                {
                    _logger.LogWarning("No album ID provided in request");
                    return BadRequest("Album ID is required.");
                }

                var albumDetails = await _spotifyService.GetAlbumDetailsAsync(albumId);
                var albumResponse = MappingHelper.MapToOutputResponse(albumDetails);
                return Ok(albumResponse);
            }
            else if (request.OutputType.Equals("track", StringComparison.OrdinalIgnoreCase))
            {
                return await ProcessTrackRequest(request);
            }

            return BadRequest("Invalid output type.");
        }

        private async Task<IActionResult> ProcessTrackRequest(OutputResponse request)
        {
            try
            {
                // Use Id if available, otherwise try to use AlbumID (for backward compatibility)
                string trackId = !string.IsNullOrEmpty(request.Id) ? request.Id : null;

                if (string.IsNullOrEmpty(trackId))
                {
                    _logger.LogWarning("No track ID provided in request");
                    return BadRequest("Track ID is required.");
                }

                var trackDetails = await _spotifyService.GetTrackDetailsAsync(trackId);
                var trackResponse = MappingHelper.MapToOutputResponse(trackDetails);
                _logger.LogInformation("Response prepared for Id: {Id}", trackId);
                return Ok(trackResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing track request for Id: {Id}", request.Id);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}