namespace SharedModels
{
    public class EvaluationPayload
    {
        public string? Classification { get; set; }
        public IntentData Intent { get; set; }
        public OutputData Output { get; set; }
    }
}
