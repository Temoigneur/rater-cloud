using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedModels.Lyrics
{
    public class LyricsIntentResponse
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

        
        public int? Popularity { get; set; }

        
        public string? ReleaseDate { get; set; }

        
        public string? PopularityRating { get; set; } // Adjusted to match JSON (integer)

        public long? PlayCount { get; set; }

        
        public double? AnnualPlayCount { get; set; }

        
        public string? Id { get; set; }

        
        public bool? IsExplicit { get; set; }

        
        public bool? IsCover { get; set; }

        
        public bool? IsRemix { get; set; }

        
        public string? ReleaseDateRaw { get; set; }
    }
}
