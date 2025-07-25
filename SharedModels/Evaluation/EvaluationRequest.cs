using System.ComponentModel.DataAnnotations;
using SharedModels.Song;

namespace SharedModels.Evaluation
{
    public class EvaluationRequest
    {
        [Required(ErrorMessage = "Classification is required.")]
        public string? Classification { get; set; }
        [Required(ErrorMessage = "Intent data is required.")]
        public IntentData Intent { get; set; }
        [Required(ErrorMessage = "Output data is required.")]
        public OutputData Output { get; set; }
    }
    public class EvaluationContext
    {
        public bool IsSimilarityQuery { get; set; }
        public string OutputType { get; set; }
        public string Query { get; set; }
        public SongFunctionalIntentResponse Intent { get; set; }
        public SongFunctionalOutputResponse Output { get; set; }
    }
}
