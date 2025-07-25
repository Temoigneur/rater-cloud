// File: Rater/Services/OpenAIService.cs

using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SharedModels.Response;


namespace Rater.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAIApiKey;
        private readonly ILogger<OpenAIService> _logger;
        private readonly ISpotifyService _spotifyService;

        // Define the instructions prompt globally
        private readonly string _instructions = @"..."; // Your existing instructions

        public OpenAIService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<OpenAIService> logger,
            ISpotifyService spotifyService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _openAIApiKey = configuration["OpenAI:ApiKey"];
            _logger = logger;
            _spotifyService = spotifyService;

            if (string.IsNullOrEmpty(_openAIApiKey))
            {
                _logger.LogError("OpenAI API key is not configured.");
                throw new InvalidOperationException(
                    "OpenAI API key is not configured. " +
                    "Please set it in appsettings.json or environment variables.");
            }
        }

        /// <summary>
        /// Determines the intent and intent type of a given query.
        /// </summary>
        /// <param name="query">The user query to analyze.</param>
        /// <param name="classification">The classification context for the query.</param>
        /// <returns>A <see cref="QueryResponse"/> containing the intent and intent type.</returns>

        public async Task<string> GetCompletionAsync(string prompt, string model = "gpt-4o-mini", float temperature = 0.5f)
{
    try
    {
        var messages = new[]
        {
            new
            {
                role = "user", 
                content = prompt
            }
        };

        var requestBody = new
        {
            model = model,
            messages = messages,
            max_tokens = 250,
            temperature = temperature
        };

        var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenAI API Error: {StatusCode} - {ErrorText}", response.StatusCode, errorText);
            return string.Empty;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
        return result?.choices?[0]?.message?.content?.ToString()?.Trim() ?? string.Empty;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in GetCompletionAsync");
        return string.Empty;
    }
}
        public async Task<bool> IsSimilarityQueryAsync(string query)
        {
            try
            {
                var messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are an assistant that determines if a query is a similarity query."
                    },
                    new
                    {
                        role = "user",
                        content = $"Is the following query a similarity query?\n\n\"{query}\""
                    }
                };

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = messages,
                    max_tokens = 100,
                    temperature = 0.0
                };

                var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API Error: {StatusCode} - {ErrorText}", response.StatusCode, errorText);
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                var answer = result?.choices?[0]?.message?.content?.ToString()?.Trim().ToLower();

                return answer == "yes" || answer == "true";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsSimilarityQueryAsync");
                return false;
            }
        }

        public async Task<SharedModels.Response.QueryResponse> DetermineIntentAsync(string query, string classification)
        {
            try
            {
                // Step 1: Clarify the query before determining intent
                var clarifiedQuery = await ClarifyQueryAsync(query);

                // Use the clarified query for intent determination
                var messages = new[]
                {
            new
            {
                role = "system",
                content = @"You are an AI assistant specialized in identifying the most likely music-related intent behind user queries.

Rules for determining intent:
- If the query contains specific song titles, artist names, album names, or recognizable lyrics, return the exact name of the song, artist, or album as the intent and specify the intent type as 'Track' or 'Album'.
- If the query is a 'Category' intent or a 'Lyrics' classification, just return the query itself with proper spelling, or if slightly unclear, return what you think the intended category intent was.
- [!!!Important!!!] Do not ignore or delete any search criterion listed within a query (e.g., for the query '2024 Slowed Rap Music' make sure you retain all three desired elements: '2024', 'Slowed', and 'Rap Music' in the final intent).
- Do not provide explanations or additional commentary—only return the intent and intent type.
- Always prioritize specific entities over general categories.
- Never make up the name of a song or track that sounds similar to the query. If you can't identify a likely intent, just return 'unknown'.

**Response Format:**
Intent: <intent>
IntentType: <Track | Album | Category | unknown>

Examples:
1. Query: 'fantasy feat old dirty bastard'
   Intent: 'Fantasy' by Mariah Carey featuring Ol' Dirty Bastard
   IntentType: Track
2. Query: 'classical music'
   Intent: Classical music
   IntentType: Category
3. Query: 'Get A Grip Aerosmith'
   Intent: 'Get A Grip' by Aerosmith
   IntentType: Album
4. Query: 'Shape of You lyrics'
   Intent: 'Shape of You' by Ed Sheeran
   IntentType: Track
"
            },
            new
            {
                role = "user",
                content = $"Determine the intent and intent type of the following query:\n\n\"{clarifiedQuery}\"\n\nPlease provide the response in the specified format."
            }
        };

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = messages,
                    max_tokens = 150,
                    temperature = 0.5
                };

                var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error from OpenAI API: {response.StatusCode} - {errorText}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("OpenAI Response: {responseContent}", responseContent);

                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                var messageContentToken = result?.choices?[0]?.message?.content;

                string intentMessage = messageContentToken != null
                    ? (string)messageContentToken
                    : null;

                if (string.IsNullOrEmpty(intentMessage))
                {
                    // Use the classification as the intent
                    _logger.LogWarning("Intent could not be determined. Using classification as intent.");
                    return new QueryResponse
                    {
                        Intent = classification ?? "unknown",
                        IntentType = "Category"
                    };
                }

                intentMessage = intentMessage.Trim();
                _logger.LogInformation("Intent Message: {IntentMessage}", intentMessage);

                // Regex to parse Intent and IntentType
                var match = Regex.Match(intentMessage, @"Intent:\s*(.+?)\s*IntentType:\s*(\w+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                {
                    string intent = match.Groups[1].Value.Trim();
                    string intentType = match.Groups[2].Value.Trim();

                    // Adjust IntentType for functional classifications
                    if (classification.EndsWith("functional", StringComparison.OrdinalIgnoreCase))
                    {
                        intentType = "Category"; // Ensure IntentType is 'Category' for functional classifications
                    }

                    // Create QueryResponse
                    var intentResponse = new QueryResponse
                    {
                        Intent = intent,
                        IntentType = intentType
                    };

                    // Return the clarified intent
                    return intentResponse;
                }
                else
                {
                    _logger.LogWarning($"Failed to parse intent and intent type from the response. Response received: '{intentMessage}'");
                    // Use the classification as the intent
                    return new QueryResponse
                    {
                        Intent = classification ?? "unknown",
                        IntentType = "Category"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DetermineIntentAsync");
                // Use classification as intent
                return new QueryResponse
                {
                    Intent = classification ?? "unknown",
                    IntentType = "Category"
                };
            }
        }

        public async Task<SharedModels.Response.IntentResponse> GetIntentDetailsAsync(IntentResponse intentResponse)
        {
            try
            {
                // Initialize the response with basic intent info.
                var intentDetails = new IntentResponse
                {
                    IntentType = intentResponse.IntentType,
                    Intent = intentResponse.Intent
                };

                if (string.IsNullOrEmpty(intentResponse.IntentType) ||
                    intentResponse.IntentType.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Intent is unknown or IntentType is null/empty. Proceeding without additional details.");
                    return intentDetails;
                }

                switch (intentResponse.IntentType.ToLowerInvariant())
                {
                    case "album":
                        if (!string.IsNullOrEmpty(intentResponse.Intent))
                        {
                            var parts = intentResponse.Intent.Split(new[] { " by " }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var albumName = parts[0].Trim();
                                var artistName = parts[1].Trim();
                                var albumResponse = await _spotifyService.SearchAlbumAsync(albumName, artistName);
                                if (albumResponse != null)
                                {
                                    intentDetails.Id = albumResponse.Id;
                                    var albumDetails = await _spotifyService.GetAlbumDetailsAsync(albumResponse.Id);
                                    if (albumDetails != null)
                                    {
                                        intentDetails.Name = albumDetails.Name ?? albumName;
                                        intentDetails.ArtistName = albumDetails.ArtistName ?? artistName;
                                        intentDetails.ReleaseDate = albumDetails.ReleaseDate;
                                        intentDetails.ReleaseDatePrecision = albumDetails.ReleaseDatePrecision; // Add this line
                                        intentDetails.Popularity = albumDetails.Popularity;
                                        intentDetails.PopularityRating = albumDetails.PopularityRating; // Use the value from albumDetails instead
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("No album found for the given query.");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Invalid format for Album Intent: {Intent}", intentResponse.Intent);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Intent is null or empty for Album IntentType.");
                        }
                        break;

                    case "track":
                        if (!string.IsNullOrEmpty(intentResponse.Intent))
                        {
                            var parts = intentResponse.Intent.Split(new[] { " by " }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var trackName = parts[0].Trim();
                                var artistName = parts[1].Trim();
                                var trackResponse = await _spotifyService.SearchTrackAsync(trackName, artistName);
                                if (trackResponse != null)
                                {
                                    intentDetails.Id = trackResponse.Id;
                                    var trackDetails = await _spotifyService.GetTrackDetailsAsync(trackResponse.Id);
                                    if (trackDetails != null)
                                    {
                                        intentDetails.Name = trackDetails.Name ?? trackName;
                                        intentDetails.ArtistName = trackDetails.ArtistName ?? artistName;
                                        intentDetails.AlbumName = trackDetails.AlbumName;
                                        intentDetails.ReleaseDate = trackDetails.ReleaseDate;
                                        intentDetails.ReleaseDatePrecision = trackDetails.ReleaseDatePrecision; // Add this line
                                        intentDetails.Popularity = trackDetails.Popularity;
                                        intentDetails.PopularityRating = trackDetails.PopularityRating; // Use the value from trackDetails instead
                                        intentDetails.IsExplicit = trackDetails.IsExplicit;

                                        // Check if the track is in the album
                                        if (!string.IsNullOrEmpty(intentDetails.AlbumName))
                                        {
                                            var albumResponse = await _spotifyService.SearchAlbumAsync(intentDetails.AlbumName, intentDetails.ArtistName);
                                            if (albumResponse != null)
                                            {
                                                var (isTrackInAlbum, _) = await _spotifyService.IsTrackInAlbumAsync(albumResponse.Id, intentDetails.Name);
                                                intentDetails.IsCover = !isTrackInAlbum;
                                                intentDetails.IsRemix = false; // Set a default value or use a heuristic to determine if it's a remix
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("No track found for the given query.");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Invalid format for Track Intent: {Intent}", intentResponse.Intent);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Intent is null or empty for Track IntentType.");
                        }
                        break;

                    default:
                        _logger.LogWarning("Unsupported IntentType: {IntentType}", intentResponse.IntentType);
                        break;
                }

                return intentDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetIntentDetailsAsync");
                return new IntentResponse
                {
                    IntentType = intentResponse.IntentType,
                    Intent = intentResponse.Intent
                };
            }
        }





        /// <summary>
        /// Extracts the title and artist name from the intent message.
        /// </summary>
        /// <param name="intentMessage">The intent message containing title and artist.</param>
        /// <returns>A tuple containing the title and artist name.</returns>
        private (string Title, string ArtistName) ExtractTitleAndArtist(string intentMessage)
        {
            if (string.IsNullOrEmpty(intentMessage))
            {
                return ("", "");
            }

            var match = Regex.Match(intentMessage, @"'(.+)' by (.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
            }
            else
            {
                string title = intentMessage.Replace("'", "").Trim();
                return (title, "");
            }
        }

        /// <summary>
        /// Clarifies and corrects any misspellings in the user query.
        /// </summary>
        /// <param name="query">The original user query.</param>
        /// <returns>The clarified and corrected query.</returns>
        public async Task<string> ClarifyQueryAsync(string query)
        {
            try
            {
                var messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = @"You are an assistant that corrects misspellings and clarifies music-related queries without changing the original meaning.

Examples:
- Input: 'beathoven sympony numbr 5'
  Output: 'Beethoven Symphony No. 5'
- Input: 'songs about luv and happiness'
  Output: 'songs about love and happiness'
- Input: 'hevy metel music except for mettallica'
  Output: 'heavy metal music except for Metallica'
- Input:  'When the blckbird in the sprong'
  Output: 'When the blackbird in the spring'"
                    },
                    new
                    {
                        role = "user",
                        content = $"Please correct any misspellings and unclear phrases without providing any explanation:\n\n\"{query}\""
                    }
                };

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = messages,
                    max_tokens = 100,
                    temperature = 0.5
                };

                var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error from OpenAI API: {response.StatusCode} - {errorText}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                var correctedQuery = result?.choices?[0]?.message?.content?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(correctedQuery))
                {
                    return correctedQuery;
                }
                else
                {
                    return query; // Return original query if clarification fails
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClarifyQueryAsync");
                return query; // Return original query if an error occurs
            }
        }
    }
}
