using Newtonsoft.Json;

namespace SharedModels.Track
{
    public class AlbumResponse
    {
        public string? AlbumID { get; set; }
        public string? AlbumName { get; set; }
        public string? ArtistName { get; set; }
    }
    public class TrackResponse
    {
        public string? Id { get; set; }
        public string? Track { get; set; }
        public string? Name { get; set; }
        public string? ArtistName { get; set; }
        public int? Popularity { get; set; }
        public string? PopularityRating { get; set; }
        public string? ReleaseDate { get; set; }
        public DateTime? ReleaseDateRaw { get; set; }
        public string? ReleaseDatePrecision { get; set; }
    }
    // Update existing SearchResponse class
    public class SearchResponse
    {
        public TracksResponse Tracks { get; set; }
    }

    // Update existing TracksResponse class
    public class TracksResponse
    {
        public List<Track> Items { get; set; }

        public class Track
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public List<ArtistInfo> Artists { get; set; }
            public AlbumSimple Album { get; set; }
            public int Popularity { get; set; }
            public bool Explicit { get; set; }
        }

        public class ArtistInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public class AlbumSimple
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string ReleaseDate { get; set; }
            public string ReleaseDatePrecision { get; set; }
        }
    }

    // Update existing Track class
    public class Track
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Artist> Artists { get; set; }
        public AlbumSimple Album { get; set; }
        public int Popularity { get; set; }
        public bool Explicit { get; set; }
    }

    // Update existing AlbumSimple class
    public class AlbumSimple
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ReleaseDate { get; set; }
        public string ReleaseDatePrecision { get; set; }
    }

    public class Artists
    {
        public List<SpotifyArtist> Artist { get; set; }
    }
    public class SpotifyArtist
    {
        


        public string Id { get; set; }
        
        public string Name { get; set; }
    }
}
