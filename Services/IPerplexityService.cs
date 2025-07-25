using System.Threading.Tasks;

namespace Rater.Services
{
    /// <summary>
    /// Interface for Perplexity AI service
    /// </summary>
    public interface IPerplexityService
    {
        /// <summary>
        /// Gets a response from Perplexity AI based on the provided prompt.
        /// </summary>
        Task<string> GetAIResponseAsync(string prompt);
    }
}
