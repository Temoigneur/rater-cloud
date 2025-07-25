using SharedModels.Artist;
using Newtonsoft.Json;

namespace SharedModels.Album
{
    // Base class with shared properties for all album responses
    public class AlbumResponse
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumID { get; set; }
        public string? ArtistName { get; set; }
        public string? ReleaseDate { get; set; } // Formatted as "MM/yyyy"
        // Change Popularity to int? to allow nullability
        public int Popularity { get; set; }
        
    }
    // Derived class for detailed album data
    public class AlbumDetails : AlbumResponse
    {
        
        public string? IntentType { get; set; }
        public string? Intent { get; set; }
        public string? Name { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumID { get; set; }
        public string? ArtistName { get; set; }
        public string? Tracks { get; set; }
        public bool TracksLoaded { get; set; }
        public int? TotalTracks { get; set; }

        public string? AlbumTracks { get; set; }
        public List<SpotifyArtist> Artists { get; set; }
        public string? ReleaseDate { get; set; }
        public string? ReleaseDatePrecision { get; set; }
        public int Popularity { get; set; }
        public string? PopularityRating { get; set; }
        public string? Id { get; set; }
        public bool? IsExplicit { get; set; }
        public bool? IsCover { get; set; }
        public bool? IsRemix { get; set; }
        
        [JsonProperty("playCount")]
        public int? PlayCount { get; set; }
        
        [JsonProperty("annualPlayCount")]
        public int? AnnualPlayCount { get; set; }
    }
    public class SpotifyAlbumSimple
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ReleaseDate { get; set; }
        public string ReleaseDatePrecision { get; set; }
    }

    public class SpotifyAlbum
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<SpotifyArtist> Artists { get; set; }
        public string ReleaseDate { get; set; }
        public string ReleaseDatePrecision { get; set; }
    }
    public class Album
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? AlbumID { get; set; }
    }

}
