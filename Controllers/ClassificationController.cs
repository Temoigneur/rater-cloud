// File: Rater\Controllers\ClassificationController.cs

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Rater.Services;
using SharedModels;


namespace Rater.Controllers
{
    [ApiController]
    [Route("api/rater/[controller]")]
    public class ClassificationController : ControllerBase
    {
        private readonly ILogger<ClassificationController> _logger;
        private readonly BackendClient _backendClient;
        private readonly ISpotifyService _spotifyService;

        private static readonly Dictionary<string, string> ClassificationToIntentTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "song_functional", "Category" },
            { "album_functional", "Category" },
            { "song_navigational", "Track" },
            { "album_navigational", "Album" },
            { "lyrics", "Lyrics" }
        };

        public ClassificationController(ILogger<ClassificationController> logger, BackendClient backendClient, ISpotifyService spotifyService)
        {
            _logger = logger;
            _backendClient = backendClient;
            _spotifyService = spotifyService;
        }

        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveClassification([FromBody] ClassificationRequest request)
        {
            try
            {
                var evaluationRequest = new SharedModels.Evaluation.EvaluationRequest
                {
                    Classification = request.Classification,
                    Intent = request.Intent,
                    Output = request.Output
                };

                if (ClassificationToIntentTypeMap.TryGetValue(evaluationRequest.Classification, out var intentType))
                {
                    evaluationRequest.Intent.IntentType = intentType;

                    if (intentType.Equals("Category", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(evaluationRequest.Intent.Query))
                        {
                            if (!string.IsNullOrWhiteSpace(evaluationRequest.Intent.Intent))
                            {
                                evaluationRequest.Intent.Query = evaluationRequest.Intent.Intent;
                                evaluationRequest.Intent.Intent = null;
                                _logger.LogInformation($"Mapped 'Intent.Intent' to 'Intent.Query' for functional classification '{evaluationRequest.Classification}'.");
                            }
                            else
                            {
                                return BadRequest(new { error = "'Intent.Query' is required for functional classifications." });
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(evaluationRequest.Intent.Intent))
                            {
                                return BadRequest(new { error = "'Intent.Intent' should not be provided for functional classifications." });
                            }
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(evaluationRequest.Intent.Intent))
                        {
                            if (!string.IsNullOrWhiteSpace(evaluationRequest.Intent.Query))
                            {
                                evaluationRequest.Intent.Intent = evaluationRequest.Intent.Query;
                                evaluationRequest.Intent.Query = null;
                                _logger.LogInformation($"Mapped 'Intent.Query' to 'Intent.Intent' for non-functional classification '{evaluationRequest.Classification}'.");
                            }
                            else
                            {
                                return BadRequest(new { error = "'Intent.Intent' is required for non-functional classifications." });
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(evaluationRequest.Intent.Query))
                            {
                                return BadRequest(new { error = "'Intent.Query' should not be provided for non-functional classifications." });
                            }
                        }
                    }
                }

                _logger.LogInformation($"Classification: {evaluationRequest.Classification}, Mapped IntentType: {evaluationRequest.Intent.IntentType}");

                var validationContext = new ValidationContext(evaluationRequest);
                var validationResults = new List<ValidationResult>(); // <--- Use the standard ValidationResult

                bool isValid = Validator.TryValidateObject(
                    evaluationRequest,
                    validationContext,
                    validationResults,
                    validateAllProperties: true
                );

                if (!isValid)
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToList();
                    _logger.LogWarning("EvaluationRequest validation failed: {Errors}", string.Join("; ", errors));
                    return BadRequest(new { errors });
                }

                _logger.LogInformation("Forwarding EvaluationRequest to Evals Backend at {URL}", "api/evals/classification");

                var responseContent = await _backendClient.PostAsync("api/evals/classification", evaluationRequest);

                _logger.LogInformation("Received response from Evals Backend: {Response}", responseContent);

                if (string.IsNullOrEmpty(responseContent))
                {
                    _logger.LogError("Empty response received from Evals backend.");
                    return StatusCode(500, "Empty response received from Evals backend.");
                }

                HttpContext.Session.SetString("Classification", request.Classification);

                return Ok(new { success = true, response = responseContent });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error in ReceiveClassification");
                return StatusCode(502, "Failed to communicate with Evals backend.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReceiveClassification");
                return StatusCode(500, "An error occurred while processing the classification.");
            }
        }
    }
}
