
using Microsoft.AspNetCore.Mvc;
using Rater.Services;
using SharedModels.Lyrics;
using SharedModels.Utilities;

namespace Rater.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LyricsController : ControllerBase
    {
        private readonly ISpotifyService _spotifyService;
        private readonly ILogger<LyricsController> _logger;

        public LyricsController(
            ISpotifyService spotifyService,
            ILogger<LyricsController> logger)
        {
            _spotifyService = spotifyService;

            _logger = logger;
        }


        private bool FuzzyMatch(string str1, string str2)
        {
            // Simple implementation - you might want to use a proper fuzzy matching library
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return false;

            str1 = str1.ToLower();
            str2 = str2.ToLower();

            // Calculate Levenshtein distance or use another similarity metric
            var distance = LevenshteinDistance(str1, str2);
            var maxLength = Math.Max(str1.Length, str2.Length);
            var similarity = 1 - ((double)distance / maxLength);

            return similarity >= 0.85; // 85% similarity threshold
        }

        private int LevenshteinDistance(string str1, string str2)
        {
            var matrix = new int[str1.Length + 1, str2.Length + 1];

            // Initialize first row and column
            for (var i = 0; i <= str1.Length; i++)
                matrix[i, 0] = i;
            for (var j = 0; j <= str2.Length; j++)
                matrix[0, j] = j;

            // Fill in the rest of the matrix
            for (var i = 1; i <= str1.Length; i++)
            {
                for (var j = 1; j <= str2.Length; j++)
                {
                    var cost = (str2[j - 1] == str1[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[str1.Length, str2.Length];
        }
    }
}
