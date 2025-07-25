using SharedModels.Interfaces;
using Newtonsoft.Json;

namespace SharedModels.Album
{
    public class AlbumNavigationalData : IAlbum
    {
        public string? IntentType { get; set; }
        public string? Intent { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public new string AlbumID { get; set; }
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public List<SpotifyArtist> Artists { get; set; }
        public new string ReleaseDate { get; set; }
        public string ReleaseDatePrecision { get; set; }
        public DateTime? ReleaseDateRaw { get; set; }
        public int? Popularity { get; set; }
        public new string PopularityRating { get; set; }
        public string? FormattedPlayCount { get; set; }
        public string? FormattedAnnualPlayCount { get; set; }
        
        [JsonProperty("playCount")]
        public int? PlayCount { get; set; }
        
        [JsonProperty("annualPlayCount")]
        public int? AnnualPlayCount { get; set; }
        
        public string? AlbumTracks { get; set; }
        public bool? IsExplicit { get; set; }
        public bool? IsCover { get; set; }
        public bool? IsRemix { get; set; }
    }
}
