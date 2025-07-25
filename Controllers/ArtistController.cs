using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Rater.Services; // Assuming your ISpotifyService is here

namespace Rater.Controllers
{
    public class ArtistSearchRequest
    {
        [Required(ErrorMessage = "artists is required.")]
        [MaxLength(100, ErrorMessage = "artists cannot exceed 100 characters.")]
        public string ArtistName { get; set; }
    }

    public class ArtistResponse
    {
        public string ArtistName { get; set; }
        public string Id { get; set; }
        public string Popularity { get; set; }
        public string OutputType { get; set; }
    }

    [ApiController]
    [Route("api/artist")]
    public class ArtistController : ControllerBase
    {
        private readonly ILogger<ArtistController> _logger;
        private readonly ISpotifyService _spotifyService;

        public ArtistController(ILogger<ArtistController> logger, ISpotifyService spotifyService)
        {
            _logger = logger;
            _spotifyService = spotifyService;
        }

        //[HttpPost("search")]
        //public async Task<IActionResult> SearchArtist([FromBody] ArtistSearchRequest request)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        _logger.LogWarning("ArtistSearchRequest validation failed: {@ModelState}", ModelState);
        //        return BadRequest(ModelState);
        //    }

        //    _logger.LogInformation("Received ArtistSearchRequest: {@Request}", request);

        //    // Get artist details (Id, Followers.Total)
        //    var artistDetails = await _spotifyService.GetArtistDetailsAsync(request.artists);

        //    if (artistDetails == null)
        //    {
        //        _logger.LogWarning("Artist not found: {artists}", request.artists);
        //        return NotFound("Artist not found.");
        //    }

        //    // artistDetails is a nullable tuple, so use .Value now that we've checked
        //    var (fetchedId, fetchedFollowers.Total) = artistDetails.Value;

        //    var Popularity = CategorizeArtistPopularity(fetchedFollowers.Total);

        //    var response = new ArtistResponse
        //    {
        //        artists = request.artists,
        //        Id = fetchedId,
        //        Followers.Total = fetchedFollowers.Total,
        //        Popularity = Popularity,
        //        OutputType = "Artist"
        //    };

        //    _logger.LogInformation("Response prepared for Artist: {artists}", request.artists);
        //    return Ok(response);
        //}

        //private string CategorizeArtistPopularity(long Followers.Total)
        //{
        //    if (Followers.Total > 5_000_000)
        //        return "Very Popular";
        //    else if (Followers.Total >= 600_000)
        //        return "Moderately Popular";
        //    else
        //        return "Unpopular";
        //}
    }
}
