using SharedModels.Artist;

namespace SharedModels.Album
{
    public class AlbumSearchResponse
    {
        public AlbumsResponse Albums { get; set; }
    }

    // Update existing AlbumsResponse class
    public class AlbumsResponse
    {
        public List<SpotifyAlbum> Items { get; set; }
    }

    // Update existing AlbumTracksResponse class
    public class AlbumTracksResponse
    {
        public List<TrackSimple> Items { get; set; }
    }
    public class TrackSimple
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<SpotifyArtist> Artists { get; set; }
    }

}
