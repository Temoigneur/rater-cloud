using System.ComponentModel.DataAnnotations;
namespace SharedModels.FunctionalQueryEvaluator
{
    public class QueryRequest
    {
        [Required(ErrorMessage = "Query is required.")]
        [MaxLength(500, ErrorMessage = "Query cannot exceed 500 characters.")]
        public string? Query { get; set; }
        [MaxLength(100, ErrorMessage = "Classification cannot exceed 100 characters.")]
        public string? Classification { get; set; }
    }
}
