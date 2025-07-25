using SpotifyAPI.Web;

namespace SharedModels.Album
{
    public class AlbumFunctionalFunctions
    {
        // Existing properties

        /// <summary>
        /// Cache of album tracks to avoid repeated API calls.
        /// </summary>
        private readonly Dictionary<string, List<string>> _albumTracksCache = new Dictionary<string, List<string>>();

        /// <summary>
        /// The Spotify client for API access.
        /// </summary>
        private readonly ISpotifyClient _spotifyClient;
    }
}
