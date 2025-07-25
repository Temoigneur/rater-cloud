using Newtonsoft.Json;
using SharedModels.Artist;
using SharedModels.Base;
using SharedModels.Common;
using SharedModels.Interfaces;

namespace SharedModels.Track
{
    public class TrackDetails : BaseResponse, ITrack
    {
        public string? Id { get; set; }
        // Remove the separate TrackId property; use Id as returned by Spotify.
        public new string Name { get; set; }
        // Use a list of artists
        public List<ArtistInfo>? Artists { get; set; }
        // Replace AlbumName and AlbumId with a nested Album object
        public AlbumInfo Album { get; set; }
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public string? AlbumID { get; set; }
        [JsonProperty("playCount")]
        public int PlayCount { get; set; }
        [JsonProperty("annualPlayCount")]
        public int? AnnualPlayCount { get; set; }
        public int? Popularity { get; set; }
        public string? PopularityRating { get; set; }
        public string? ReleaseDate { get; set; }
        public string? ReleaseDatePrecision { get; set; }
        public bool IsExplicit { get; set; }
        public bool IsCover { get; set; }
        public bool IsRemix { get; set; }
        public string? TrackId { get; set; }
        public string PreviewUrl { get; set; }
        public Dictionary<string, string> ExternalUrls { get; set; }
    }

    public class SpotifyTrack
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<SpotifyArtist> Artists { get; set; }
        public SpotifyAlbumSimple Album { get; set; }
        public int Popularity { get; set; }
        public bool Explicit { get; set; }
    }

    public class SpotifyTrackSimple
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<SpotifyArtist> Artists { get; set; }
    }
    public class AlbumTrack
    {
        
        public string Name { get; set; }

        
        public Artist[] Artists { get; set; }
    }

    public class SpotifyAlbumSimple
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? AlbumType { get; set; }
        public List<string>? AvailableMarkets { get; set; }
        public ExternalUrls? ExternalUrls { get; set; }
        public string? Href { get; set; }
        public List<SpotifyImage>? Images { get; set; }
        public string? ReleaseDate { get; set; }
        public string? ReleaseDatePrecision { get; set; }
        public int? TotalTracks { get; set; }
        public string? Type { get; set; }
        public string? Uri { get; set; }
    }

    public class ExternalUrls
    {
        public string? Spotify { get; set; }
    }

    public class SpotifyImage
    {
        public int? Height { get; set; }
        public int? Width { get; set; }
        public string? Url { get; set; }
    }
    public class ArtistInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
