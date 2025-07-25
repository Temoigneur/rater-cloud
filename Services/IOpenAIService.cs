// File: SharedModels/Interfaces/IOpenAIService.cs

using SharedModels.Response;

namespace Rater.Services
{
    public interface IOpenAIService
    {

        Task<QueryResponse> DetermineIntentAsync(string query, string classification);
        Task<string> ClarifyQueryAsync(string query);
        Task<SharedModels.Response.IntentResponse> GetIntentDetailsAsync(IntentResponse intentResponse);
        Task<bool> IsSimilarityQueryAsync(string query); // Add this method
        // Add other method signatures as needed
        // Add to IOpenAIService.cs
Task<string> GetCompletionAsync(string prompt, string model = "gpt-4o-mini", float temperature = 0.5f);
    }
}


