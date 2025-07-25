using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using SharedModels.Common;  // For AlbumInfo and ArtistInfo

namespace SharedModels
{
    public class IntentData
    {

        public string? Intent { get; set; }


        public string? Query { get; set; }


        [Required(ErrorMessage = "IntentType is required.")]
        [RegularExpression("(?i)track|album|category|unknown|artist", ErrorMessage = "IntentType must be 'track', 'album', 'category', 'unknown', or 'artist'.")]
        public string IntentType { get; set; }

        // Replace AlbumName and AlbumID with a nested Album object

        public AlbumInfo? Album { get; set; }

        // Replace ArtistName with a list of artists

        public List<ArtistInfo>? Artists { get; set; }

        public string? Name { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumID { get; set; }
        // Spotify API returns popularity as an integer (0–100)
        
        public int? Popularity { get; set; }

        
        public string? PopularityRating { get; set; }

        
        public string? ReleaseDate { get; set; }

        
        public string? Id { get; set; }

        
        public bool? IsExplicit { get; set; }

        
        public bool? IsCover { get; set; }

        
        public bool? IsRemix { get; set; }

        
        public string? ReleaseDateRaw { get; set; }

        [JsonProperty("playCount")]
        public int? PlayCount { get; set; }

        [JsonProperty("annualPlayCount")]
        public int? AnnualPlayCount { get; set; }
    }
}
