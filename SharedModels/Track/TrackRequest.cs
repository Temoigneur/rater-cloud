using System.ComponentModel.DataAnnotations;
using SharedModels.Interfaces;
namespace SharedModels.Track
{
    public class TrackRequest : ITrack
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string? Name { get; set; }
        [MaxLength(100, ErrorMessage = "artists cannot exceed 100 characters.")]
        public string? ArtistName { get; set; }
        public string? TrackId { get; set; }
    }
}
