using Newtonsoft.Json;

namespace SharedModels.Evaluation
{
    public class EvaluationResult
    {
        

        public string? Result { get; set; }

        
        public string? Rating { get; private set; }

        
        public string? Explanation { get; private set; }

        
        public string? Justification { get; private set; }

        public EvaluationResult(string result)
        {
            Result = result;
        }

        public EvaluationResult(string rating, string explanation, string? justification = null)
        {
            Rating = rating;
            Explanation = explanation;
            Justification = justification;
            Result = FormatResult();
        }

        private string FormatResult()
        {
            if (string.IsNullOrEmpty(Rating) || string.IsNullOrEmpty(Explanation))
                return string.Empty;

            var resultLines = new List<string>();

            // Add explanation if available
            if (!string.IsNullOrEmpty(Explanation))
                resultLines.Add($"-{Explanation}");

            // Add justification if available
            if (!string.IsNullOrEmpty(Justification))
                resultLines.Add($"-{Justification}");

            // Add rating
            resultLines.Add($"-Rating = {Rating}");

            return string.Join("\n", resultLines);
        }
    }
}

