namespace SharedModels.Evaluation
{
    public abstract class EvaluationPayloadBase
    {
        public string? Classification { get; set; }
    }
    public class EvaluationPayload<TIntentResponse, TOutputResponse> : EvaluationPayloadBase
    {
        public TIntentResponse Intent { get; set; }
        public TOutputResponse Output { get; set; }
    }
    public class EvaluationPayload : EvaluationPayloadBase
    {
        public object Intent { get; set; }
        public object Output { get; set; }
    }
}
