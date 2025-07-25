using Newtonsoft.Json;
using SharedModels.Common;

namespace SharedModels
{
    public class OutputData
    {
        [JsonProperty("playCount")]
        public int? PlayCount { get; set; }

        [JsonProperty("annualPlayCount")]
        public int? AnnualPlayCount { get; set; }


        public string OutputType { get; set; }

        // Use a nested Album object instead of separate AlbumName/AlbumID
        
        public AlbumInfo? Album { get; set; }

        // Use a list of artists instead of a single ArtistName string
        
        public List<ArtistInfo>? Artists { get; set; }

        
        public string? Name { get; set; }
        public string? AlbumName { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumID { get; set; }


        
        public string? Id { get; set; }

        
        public int? Popularity { get; set; }

        
        public string? PopularityRating { get; set; }

        
        public string? ReleaseDate { get; set; }
        
        public string? ReleaseDatePrecision { get; set; }

        // Adjusted to match JSON (integer)

        
        public bool? IsExplicit { get; set; }

        
        public bool? IsCover { get; set; }

        
        public bool? IsRemix { get; set; }

        ////
        //public string LyricsMatchMarker { get; set; }
    }
    public class SpotifyArtist
    {
        
        public string Id { get; set; }
        
        public string Name { get; set; }
    }
}