namespace SharedModels.Perplexity
{
    public class PerplexityResponse<T>
    {
        public T Data { get; }
        public string RawResponse { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public PerplexityResponse(T data, string rawResponse = null)
        {
            Data = data;
            RawResponse = rawResponse;
            IsSuccess = true;
        }

        public PerplexityResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
            IsSuccess = false;
        }
    }

    public class SimilarityQueryResult
    {
        public string Answer { get; set; }
        public string Justification { get; set; }
    }

}
