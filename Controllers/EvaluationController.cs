// File: Rater/Controllers/EvaluationController.cs
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SharedModels;

namespace Rater.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvaluationController : ControllerBase
    {
        private readonly ILogger<EvaluationController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public EvaluationController(ILogger<EvaluationController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Receives an evaluation payload and forwards it to the SortingController in the second backend.
        /// </summary>
        /// <param name="payload">The evaluation payload containing classification and related data.</param>
        /// <returns>A response indicating success or failure.</returns>
        [HttpPost]
        public async Task<IActionResult> Evaluate([FromBody] EvaluationPayload payload)
        {
            try
            {
                if (payload == null)
                {
                    _logger.LogWarning("Evaluation payload is required.");
                    return BadRequest("Evaluation payload is required.");
                }

                // Send the evaluation payload to the SortingController in the second backend
                var client = _httpClientFactory.CreateClient();
                var sortingControllerUrl = "http://localhost:5236/api/Sorting"; // Update the URL as needed

                // Configure serialization settings to include type information
                var jsonSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                };

                var jsonPayload = JsonConvert.SerializeObject(payload, jsonSettings);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(sortingControllerUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var resultContent = await response.Content.ReadAsStringAsync();
                    return Ok(resultContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error in SortingController: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, "Error during evaluation.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Evaluate");
                return StatusCode(500, "An error occurred during evaluation.");
            }
        }
    }
}
