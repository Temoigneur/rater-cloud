using Newtonsoft.Json;
using SharedModels.Common;
using System.Collections.Generic;

namespace SharedModels.Response
{
    public class IntentResponse
    {
        

        public string? Intent { get; set; }

        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public string? ArtistId { get; set; }
        public bool IsCover { get; set; }
        public bool IsRemix { get; set; }

        
        public string? Query { get; set; }

        
        public string IntentType { get; set; }

        // Use a nested Album object for album details
        public string? Tracks { get; set; }
        public AlbumInfo? Album { get; set; }

        // Use a list for artist details
        
        public List<ArtistInfo>? Artists { get; set; }

        
        public string? Id { get; set; }

        
        public string? Name { get; set; }

        
        public int? Popularity { get; set; }

        
        public string? ReleaseDate { get; set; }
        public string? ReleaseDatePrecision { get; set; }
        public string? PopularityRating { get; set; }
        public bool? IsExplicit { get; set; }

        // Play count properties - single declaration with JsonProperty attribute
        [JsonProperty("playCount")]
        public int PlayCount { get; set; }

        [JsonProperty("annualPlayCount")]
        public int AnnualPlayCount { get; set; }
    }
}
