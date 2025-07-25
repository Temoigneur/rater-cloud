using SharedModels.Evaluation;
namespace SharedModels.Interfaces
{
    public interface IEvaluator
    {
        Task<string> EvaluateAsync(EvaluationRequest request);
    }
}
