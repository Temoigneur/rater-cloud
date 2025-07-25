using SharedModels.Track;
using SpotifyAPI.Web;

namespace Rater.Services
{
    public interface ISpotifyService
    {
        
        Task<SharedModels.Track.TrackDetails> GetTrackDetailsAsync(string Id);
        Task<TrackDetails> GetCachedTrackDetailsAsync(string Id);
        Task<SharedModels.Album.AlbumResponse> SearchAlbumAsync(string Name, string artistName);
        Task<List<SimpleAlbum>> SearchAlbumAsync(string albumName);
        Task<SharedModels.Album.AlbumDetails> GetAlbumDetailsAsync(string Id);
        Task<FullArtist> GetArtistAsync(string id);

        Task<SpotifyAPI.Web.SearchResponse> Search(SearchRequest searchRequest);
        Task<TrackResponse> SearchTrackAsync(string name, string artistName);
        Task<List<FullTrack>> GetTracksAsync(string query, int limit = 8);
        Task<(bool isTrackInAlbum, string matchingTrackName)> IsTrackInAlbumAsync(string albumId, string intentTrackName);
        string CategorizePopularity(double popularity);
        string CategorizeAlbumPopularity(int popularity);
    }
}