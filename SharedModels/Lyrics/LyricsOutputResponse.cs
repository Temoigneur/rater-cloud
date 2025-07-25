using Newtonsoft.Json;

namespace SharedModels.Lyrics
{
    public class LyricsOutputResponse
    {
        

        public string OutputType { get; set; }

        
        public string? AlbumName { get; set; }

        
        public string? ArtistName { get; set; }

        
        public string? Name { get; set; }

        
        public string? AlbumID { get; set; }

        
        public int? Popularity { get; set; }

        
        public string? ReleaseDate { get; set; }

        
        public string? PopularityRating { get; set; } // Adjusted to match JSON (integer)

        
        public string? Id { get; set; }

        
        public bool? IsExplicit { get; set; }

        
        public bool? IsCover { get; set; }

        
        public bool? IsRemix { get; set; }

        
        public string? ReleaseDateRaw { get; set; }
        
        public long? MonthlyListeners { get; set; }
        
        public string? ArtistPopularityRating { get; set; }
    }
}
