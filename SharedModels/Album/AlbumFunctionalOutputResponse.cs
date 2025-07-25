using Newtonsoft.Json;

namespace SharedModels.Album
{
    public class AlbumFunctionalOutputResponse : AlbumFunctionalData
    {
        public string? OutputType { get; set; }
        public string? Name { get; set; }
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The Spotify ID of the album this track belongs to.
        /// </summary>
        public string AlbumId { get; set; }

        /// <summary>
        /// The track number within the album.
        /// </summary>
        public int? TrackNumber { get; set; }

        /// <summary>
        /// The disc number within the album.
        /// </summary>
        public int? DiscNumber { get; set; }
        public int? Popularity { get; set; }
        public string? PopularityRating { get; set; }
        public string? ReleaseDate { get; set; }
        public string? AlbumID { get; set; }
        public bool? IsExplicit { get; set; }
        public bool? IsCover { get; set; }
        public bool? IsRemix { get; set; }
        
        [JsonProperty("playCount")]
        public int? PlayCount { get; set; }
        
        [JsonProperty("annualPlayCount")]
        public int? AnnualPlayCount { get; set; }
    }
}
