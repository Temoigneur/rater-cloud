using System.ComponentModel.DataAnnotations;
namespace SharedModels.Request
{
    public class OutputRequest
    {
        public string? Id { get; set; }
        
        [Required(ErrorMessage = "OutputType is required.")]
        [RegularExpression("^(Track|Album|Category)$", ErrorMessage = "OutputType must be 'Track', 'Album', or 'Category'.")]
        public string? OutputType { get; set; }
        
        public string? AlbumID { get; set; }
        public string? AlbumName { get; set; }
        public string? ArtistName { get; set; }
        public string? ArtistID { get; set; }
        public double? PopularityScore { get; set; }
        public string? PopularityRating { get; set; }
        public string? ReleaseDate { get; set; }
        public string? Name { get; set; }
    }
}
