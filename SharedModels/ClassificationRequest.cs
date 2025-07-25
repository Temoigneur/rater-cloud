using System.ComponentModel.DataAnnotations;
namespace SharedModels
{
    public class ClassificationRequest
    {
        [Required(ErrorMessage = "Classification is required.")]
        [MaxLength(100, ErrorMessage = "Classification cannot exceed 100 characters.")]
        public string? Classification { get; set; }
        public SharedModels.IntentData Intent { get; set; }
        public SharedModels.OutputData Output { get; set; }
    }
}
