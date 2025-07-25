using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedModels.Song
{
    public class SongNavigationalIntentResponse
    {
        
        public string? Intent { get; set; }

        
        public string? Query { get; set; }

        
        [Required(ErrorMessage = "IntentType is required.")]
        [RegularExpression("(?i)Track|Album|Category|Artist", ErrorMessage = "IntentType must be 'Track', 'Album', 'Category', 'Artist'.")]
        public string IntentType { get; set; }

        // Optional Properties
        public string? AlbumName { get; set; }

        
        public string? ArtistName { get; set; }

        
        public string? Name { get; set; }

        
        public string? AlbumID { get; set; }

        
        public double? Popularity { get; set; }

        
        public string? PopularityRating { get; set; }

        
        public string? ReleaseDate { get; set; }
		public string? ReleaseDatePrecise { get; set; }


        // Adjusted to match JSON (integer)


        
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
