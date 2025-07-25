using System.ComponentModel.DataAnnotations;
namespace SharedModels.Album
{
    public class AlbumSearchRequest
    {
        [Required(ErrorMessage = "AlbumName is required.")]
        [MaxLength(100, ErrorMessage = "AlbumName cannot exceed 100 characters.")]
        public string? AlbumName { get; set; }
        [MaxLength(100, ErrorMessage = "artists cannot exceed 100 characters.")]
        public string? ArtistName { get; set; }
        public string? AlbumID { get; set; }
        public string? Name { get; set; }
    }
}
