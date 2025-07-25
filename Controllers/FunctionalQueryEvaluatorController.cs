// File: Rater/Controllers/FunctionalQueryEvaluatorController.cs

using Microsoft.AspNetCore.Mvc;
using Rater.Services;
using SharedModels.FunctionalQueryEvaluator; // Updated namespace

namespace Rater.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FunctionalQueryEvaluatorController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<FunctionalQueryEvaluatorController> _logger;

        public FunctionalQueryEvaluatorController(IOpenAIService openAIService, ILogger<FunctionalQueryEvaluatorController> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
        }

        /// <summary>
        /// Evaluates whether the given query is a similarity query using OpenAIService.
        /// </summary>
        /// <param name="request">The query request containing the user query.</param>
        /// <returns>A boolean indicating if it's a similarity query.</returns>
        [HttpPost]
        public async Task<IActionResult> EvaluateSimilarity([FromBody] QueryRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Query))
                {
                    _logger.LogWarning("Query is required.");
                    return BadRequest("Query is required.");
                }

                bool isSimilarityQuery = await _openAIService.IsSimilarityQueryAsync(request.Query);

                return Ok(new { isSimilarityQuery });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EvaluateSimilarity");
                return StatusCode(500, "An error occurred while evaluating the query.");
            }
        }
    }
}