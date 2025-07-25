using Microsoft.Extensions.Caching.Memory;
using SharedModels.Track;
using SharedModels.Common;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rater.Services
{
    public class SpotifyService : ISpotifyService
    {
        private readonly ILogger<SpotifyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private SpotifyClient _spotifyClient;
        private readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1, 1);
        private DateTime _tokenExpirationTime;
        private readonly IApifyService _apifyService;

        public SpotifyService(IConfiguration configuration, ILogger<SpotifyService> logger, IMemoryCache cache, IApifyService apifyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _apifyService = apifyService ?? throw new ArgumentNullException(nameof(apifyService));

            try
            {
                InitializeSpotifyClientAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Spotify client");
                throw;
            }
        }

        private async Task InitializeSpotifyClientAsync()
        {
            var clientId = _configuration["Spotify:ClientId"];
            var clientSecret = _configuration["Spotify:ClientSecret"];

            if (string.IsNullOrEmpty(clientId))
                throw new InvalidOperationException("Spotify Client ID is missing");

            if (string.IsNullOrEmpty(clientSecret))
                throw new InvalidOperationException("Spotify Client Secret is missing");

            var config = SpotifyClientConfig.CreateDefault();
            var request = new ClientCredentialsRequest(clientId, clientSecret);
            var response = await new OAuthClient(config).RequestToken(request);

            _spotifyClient = new SpotifyClient(config.WithToken(response.AccessToken));

            // Set expiration time (typically 3600 seconds/1 hour for Spotify)
            _tokenExpirationTime = DateTime.UtcNow.AddSeconds(response.ExpiresIn - 300); // Refresh 5 minutes early

            _logger.LogInformation("Spotify client initialized. Token expires at: {ExpirationTime}", _tokenExpirationTime);
        }

        /// <summary>
        /// Refreshes the Spotify access token using client credentials flow.
        /// </summary>
        public async Task RefreshTokenAsync()
        {
            try
            {
                // Use semaphore to prevent multiple simultaneous refreshes
                await _tokenSemaphore.WaitAsync();

                _logger.LogInformation("Refreshing Spotify token.");
                var clientId = _configuration["Spotify:ClientId"];
                var clientSecret = _configuration["Spotify:ClientSecret"];

                if (string.IsNullOrEmpty(clientId))
                    throw new InvalidOperationException("Spotify Client ID is missing during refresh");

                if (string.IsNullOrEmpty(clientSecret))
                    throw new InvalidOperationException("Spotify Client Secret is missing during refresh");

                var config = SpotifyClientConfig.CreateDefault();
                var request = new ClientCredentialsRequest(clientId, clientSecret);
                var response = await new OAuthClient(config).RequestToken(request);

                _spotifyClient = new SpotifyClient(config.WithToken(response.AccessToken));

                // Set expiration time (typically 3600 seconds/1 hour for Spotify)
                _tokenExpirationTime = DateTime.UtcNow.AddSeconds(response.ExpiresIn - 300); // Refresh 5 minutes early

                _logger.LogInformation("Spotify token refreshed successfully. Expires at: {ExpirationTime}", _tokenExpirationTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Spotify token.");
                throw;
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        // Add a method to check if token needs refresh
        private async Task EnsureValidTokenAsync()
        {
            if (DateTime.UtcNow >= _tokenExpirationTime)
            {
                await RefreshTokenAsync();
            }
        }

        public async Task<List<FullTrack>> GetTracksAsync(string query, int limit = 8)
        {
            try
            {
                await EnsureValidTokenAsync();

                _logger.LogInformation("Searching for tracks with query: {Query}, limit: {Limit}", query, limit);

                // Search for tracks with the given query
                var searchRequest = new SearchRequest(SearchRequest.Types.Track, query)
                {
                    Limit = limit,
                    Market = "US" // You can change this or make it configurable
                };

                var searchResponse = await _spotifyClient.Search.Item(searchRequest);
                if (searchResponse?.Tracks?.Items == null || !searchResponse.Tracks.Items.Any())
                {
                    _logger.LogWarning("No tracks found for query: {Query}", query);
                    return new List<FullTrack>();
                }

                // Sort by popularity (highest first)
                var sortedTracks = searchResponse.Tracks.Items
                    .OrderByDescending(t => t.Popularity)
                    .Take(limit)
                    .ToList();

                _logger.LogInformation("Found {Count} tracks for query: {Query}", sortedTracks.Count, query);
                return sortedTracks;
            }
            catch (APIException ex) when (ex.Message.Contains("401"))
            {
                // Handle expired token specifically
                _logger.LogWarning("Token appears to be expired. Refreshing and retrying...");
                await RefreshTokenAsync();
                return await GetTracksAsync(query, limit); // Retry after refresh
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTracksAsync for query: {Query}", query);
                throw;
            }
        }

        public async Task<TrackResponse> SearchTrackAsync(string name, string artistName)
        {
            var query = string.IsNullOrWhiteSpace(artistName) ? name : $"{name} {artistName}";
            var searchRequest = new SearchRequest(SearchRequest.Types.Track, query)
            {
                Limit = 1
            };

            try
            {
                await EnsureValidTokenAsync();

                var searchResponse = await _spotifyClient.Search.Item(searchRequest);
                if (searchResponse?.Tracks?.Items == null)
                {
                    _logger.LogWarning("Search response or tracks collection was null for query: {Query}", query);
                    return null;
                }

                if (searchResponse.Tracks.Items.Count > 0)
                {
                    var track = searchResponse.Tracks.Items[0];
                    return new TrackResponse
                    {
                        Id = track.Id,
                        Name = track.Name,
                        ArtistName = string.Join(", ", track.Artists.Select(a => a.Name)),
                        Popularity = track.Popularity,
                        ReleaseDate = null
                    };
                }

                _logger.LogWarning("No tracks found for query: {Query}", query);
                return null;
            }
            catch (APIException ex) when (ex.Message.Contains("401"))
            {
                // Handle expired token specifically
                _logger.LogWarning("Token appears to be expired. Refreshing and retrying...");
                await RefreshTokenAsync();
                return await SearchTrackAsync(name, artistName); // Retry after refresh
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchTrackAsync for query: {Query}", query);
                throw;
            }
        }

        public async Task<SharedModels.Track.TrackDetails> GetTrackDetailsAsync(string Id)
        {
            try
            {
                string cacheKey = $"track_{Id}";
                if (_cache.TryGetValue(cacheKey, out SharedModels.Track.TrackDetails cachedTrack))
                {
                    _logger.LogInformation("Retrieved track details from cache for Id: {Id}", Id);
                    
                    // Make sure cached track has artist name set
                    if (string.IsNullOrEmpty(cachedTrack.ArtistName) && cachedTrack.Artists != null && cachedTrack.Artists.Any())
                    {
                        cachedTrack.ArtistName = string.Join(", ", cachedTrack.Artists.Select(a => a.Name));
                        _logger.LogWarning("Artist name missing in cached track, setting from Artists collection: {ArtistName}", cachedTrack.ArtistName);
                    }
                    
                    return cachedTrack;
                }

                await EnsureValidTokenAsync();

                var track = await _spotifyClient.Tracks.Get(Id);
                if (track == null)
                {
                    _logger.LogWarning("Track not found for Id: {Id}", Id);
                    return null;
                }

                _logger.LogInformation("Retrieved track from Spotify API - Name: {Name}, Artists: {Artists}", 
                    track.Name, 
                    track.Artists != null ? string.Join(", ", track.Artists.Select(a => a.Name)) : "None");

                // Ensure we have artists data
                if (track.Artists == null || !track.Artists.Any())
                {
                    _logger.LogWarning("Track has no artists information from Spotify API: {Id}", Id);
                }

                string artistNameString = "Unknown Artist";
                if (track.Artists != null && track.Artists.Any())
                {
                    artistNameString = string.Join(", ", track.Artists.Select(a => a.Name));
                    _logger.LogInformation("Setting artist name for track: {ArtistName}", artistNameString);
                }

                var trackDetails = new SharedModels.Track.TrackDetails
                {
                    Id = track.Id,
                    Name = track.Name,
                    Popularity = track.Popularity,
                    IsExplicit = track.Explicit,
                    IsCover = false,  // This would need to be determined through additional logic
                    IsRemix = track.Name.Contains("remix", StringComparison.OrdinalIgnoreCase) || track.Name.Contains("mix", StringComparison.OrdinalIgnoreCase),
                    ReleaseDate = track.Album?.ReleaseDate,
                    ReleaseDatePrecision = track.Album?.ReleaseDatePrecision,
                    ArtistName = artistNameString,
                    Artists = track.Artists?.Select(a => new SharedModels.Track.ArtistInfo
                    {
                        Id = a.Id,
                        Name = a.Name
                    }).ToList(),
                    AlbumName = track.Album?.Name,
                    AlbumID = track.Album?.Id,
                    Album = new AlbumInfo
                    {
                        Id = track.Album?.Id,
                        Name = track.Album?.Name,
                        ReleaseDate = track.Album?.ReleaseDate,
                        ReleaseDatePrecision = track.Album?.ReleaseDatePrecision
                    },
                    PopularityRating = CategorizePopularity(track.Popularity),
                    ExternalUrls = track.ExternalUrls ?? new Dictionary<string, string>()
                };

                // Verify artist name was set correctly
                if (string.IsNullOrEmpty(trackDetails.ArtistName))
                {
                    trackDetails.ArtistName = "Unknown Artist";
                    _logger.LogWarning("Artist name is empty after creation, setting default: {ArtistName}", trackDetails.ArtistName);
                }
                else
                {
                    _logger.LogInformation("Artist name set successfully: {ArtistName}", trackDetails.ArtistName);
                }

                // Fetch play count data from Apify
                try
                {
                    string trackUrl = $"https://open.spotify.com/track/{Id}";
                    var playCountsDict = await _apifyService.GetPlayCountsAsync(new List<string> { trackUrl });
                    
                    if (playCountsDict.TryGetValue(trackUrl, out var playCount) && playCount.HasValue)
                    {
                        trackDetails.PlayCount = playCount.Value;
                        
                        // Calculate annual play count
                        if (!string.IsNullOrEmpty(track.Album?.ReleaseDate))
                        {
                            trackDetails.AnnualPlayCount = CalculateAnnualPlayCount(
                                playCount.Value, 
                                track.Album.ReleaseDate, 
                                track.Album.ReleaseDatePrecision);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not retrieve play count for track ID: {Id}", Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving play count data for track ID: {Id}", Id);
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };

                // Final check for artist name before caching
                if (string.IsNullOrEmpty(trackDetails.ArtistName) && trackDetails.Artists != null && trackDetails.Artists.Any())
                {
                    trackDetails.ArtistName = string.Join(", ", trackDetails.Artists.Select(a => a.Name));
                    _logger.LogWarning("Artist name missing before caching, setting from Artists collection: {ArtistName}", trackDetails.ArtistName);
                }

                _cache.Set(cacheKey, trackDetails, cacheEntryOptions);
                _logger.LogInformation("Added track details to cache for Id: {Id}, Artist: {ArtistName}", Id, trackDetails.ArtistName);

                return trackDetails;
            }
            catch (APIException ex) when (ex.Message.Contains("401"))
            {
                // Handle expired token specifically
                _logger.LogWarning("Token appears to be expired. Refreshing and retrying...");
                await RefreshTokenAsync();
                return await GetTrackDetailsAsync(Id); // Retry after refresh
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTrackDetailsAsync for Id: {Id}", Id);
                throw;
            }
        }

        public async Task<TrackDetails> GetCachedTrackDetailsAsync(string Id)
        {
            return await _cache.GetOrCreateAsync(Id, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await GetTrackDetailsAsync(Id);
            });
        }

        public async Task<List<SimpleAlbum>> SearchAlbumAsync(string albumName)
        {
            try
            {
                await EnsureValidTokenAsync();

                var searchRequest = new SearchRequest(SearchRequest.Types.Album, albumName);
                var searchResponse = await _spotifyClient.Search.Item(searchRequest);

                if (searchResponse.Albums != null && searchResponse.Albums.Items != null)
                {
                    return searchResponse.Albums.Items.ToList();
                }

                return new List<SimpleAlbum>();
            }
            catch (APIException ex) when (ex.Message.Contains("401"))
            {
                // Handle expired token specifically
                _logger.LogWarning("Token appears to be expired. Refreshing and retrying...");
                await RefreshTokenAsync();
                return await SearchAlbumAsync(albumName); // Retry after refresh
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchAlbumAsync for albumName: {AlbumName}", albumName);
                throw;
            }
        }

        public async Task<SharedModels.Album.AlbumResponse> SearchAlbumAsync(string name, string artistName)
        {
            var query = string.IsNullOrWhiteSpace(artistName) ? name : $"{name} {artistName}";
            var searchRequest = new SearchRequest(SearchRequest.Types.Album, query)
            {
                Limit = 1
            };

            try
            {
                await EnsureValidTokenAsync();

                var searchResponse = await _spotifyClient.Search.Item(searchRequest);
                if (searchResponse?.Albums?.Items == null)
                {
                    _logger.LogWarning("Search response or albums collection was null for query: {Query}", query);
                    return null;
                }

                if (searchResponse.Albums.Items.Count > 0)
                {
                    var album = searchResponse.Albums.Items[0];
                    return new SharedModels.Album.AlbumResponse
                    {
                        Id = album.Id,
                        Name = album.Name,
                        ArtistName = string.Join(", ", album.Artists.Select(a => a.Name))
                    };
                }

                _logger.LogWarning("No albums found for query: {Query}", query);
                return null;
            }
            catch (APIException ex) when (ex.Message.Contains("401"))
            {
                // Handle expired token specifically
                _logger.LogWarning("Token appears to be expired. Refreshing and retrying...");
                await RefreshTokenAsync();
                return await SearchAlbumAsync(name, artistName); // Retry after refresh
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchAlbumAsync for query: {Query}", query);
                throw;
            }
        }

        public async Task<SharedModels.Album.AlbumDetails> GetAlbumDetailsAsync(string id)
        {
            try
            {
                await EnsureValidTokenAsync();

                var album = await _spotifyClient.Albums.Get(id);
                if (album == null)
                {
                    _logger.LogWarning("Album not found for Id: {Id}", id);
                    return null;
                }

                var artistName = (album.Artists != null && album.Artists.Count > 0)
                    ? string.Join(", ", album.Artists.Select(a => a.Name))
                    : "Unknown Artist";

                var releaseDate = album.ReleaseDate;
                if (string.IsNullOrWhiteSpace(releaseDate))
                {
                    _logger.LogWarning("ReleaseDate not found for album Id: {Id}", id);
                    releaseDate = null;
                }

                int? albumPopularity = album.Popularity;

                return new SharedModels.Album.AlbumDetails
                {
                    Id = album.Id,
                    Name = album.Name,
                    ArtistName = artistName,
                    ReleaseDate = releaseDate,
                    ReleaseDatePrecision = album.ReleaseDatePrecision,
                    Popularity = albumPopularity ?? 0,
                    PopularityRating = CategorizeAlbumPopularity((int)(albumPopularity ?? 0))
                };
            }
            catch (APIException ex) when (ex.Message.Contains("401"))
            {
                // Handle expired token specifically
                _logger.LogWarning("Token appears to be expired. Refreshing and retrying...");
                await RefreshTokenAsync();
                return await GetAlbumDetailsAsync(id); // Retry after refresh
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAlbumDetailsAsync for Id: {Id}", id);
                throw;
            }
        }

        public async Task<(bool isTrackInAlbum, string matchingTrackName)> IsTrackInAlbumAsync(string albumId, string intentTrackName)
        {
            try
            {
                await EnsureValidTokenAsync();

                var tracksResponse = await _spotifyClient.Albums.GetTracks(albumId);
                if (tracksResponse?.Items == null)
                {
                    _logger.LogWarning("Album tracks not found for AlbumID: {AlbumID}", albumId);
                    return (false, null);
                }

                foreach (var track in tracksResponse.Items)
                {
                    var trackNameLower = track.Name.ToLowerInvariant();
                    var intentTrackNameLower = intentTrackName.ToLowerInvariant();

                    if (trackNameLower.Contains(intentTrackNameLower) ||
                        intentTrackNameLower.Contains(trackNameLower) ||
                        LevenshteinDistance(trackNameLower, intentTrackNameLower) <= 3)
                    {
                        _logger.LogInformation("Found matching track '{MatchingTrackName}' for intent '{IntentTrackName}' in album {AlbumId}",
                            track.Name, intentTrackName, albumId);

                        return (true, track.Name);
                    }
                }

                _logger.LogInformation("No matching track found for intent '{IntentTrackName}' in album {AlbumId}",
                    intentTrackName, albumId);

                return (false, null);
            }
            catch (APIException ex) when (ex.Message.Contains("401"))
            {
                // Handle expired token specifically
                _logger.LogWarning("Token appears to be expired. Refreshing and retrying...");
                await RefreshTokenAsync();
                return await IsTrackInAlbumAsync(albumId, intentTrackName); // Retry after refresh
            }
            catch (APIException ex)
            {
                _logger.LogError(ex, "Error while checking if track is in album on Spotify.");
                return (false, null);
            }
        }

        public async Task<FullArtist> GetArtistAsync(string id)
        {
            try
            {
                await EnsureValidTokenAsync();

                _logger.LogInformation("Getting artist info for ID: {Id}", id);

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Artist ID is null or empty");
                    return null;
                }

                var artist = await _spotifyClient.Artists.Get(id);
                if (artist == null)
                {
                    _logger.LogWarning("Artist data not found for Id: {Id}", id);
                    return null;
                }

                return artist;
            }
            catch (APIException ex) when (ex.Message.Contains("401"))
            {
                // Handle expired token specifically
                _logger.LogWarning("Token appears to be expired. Refreshing and retrying...");
                await RefreshTokenAsync();
                return await GetArtistAsync(id); // Retry after refresh
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetArtistAsync for Id: {Id}", id);
                throw;
            }
        }

        public async Task<SpotifyAPI.Web.SearchResponse> Search(SearchRequest searchRequest)
        {
            try
            {
                await EnsureValidTokenAsync();

                var searchResponse = await _spotifyClient.Search.Item(searchRequest);
                return searchResponse;
            }
            catch (APIException ex) when (ex.Message.Contains("401"))
            {
                // Handle expired token specifically
                _logger.LogWarning("Token appears to be expired. Refreshing and retrying...");
                await RefreshTokenAsync();
                return await Search(searchRequest); // Retry after refresh
            }
            catch (APIException ex)
            {
                _logger.LogError(ex, "Error occurred while searching with Spotify API");
                throw;
            }
        }

        public string CategorizePopularity(double popularity)
        {
            try
            {
                if (popularity <= 20)
                    return "unpopular";
                else if (popularity <= 40)
                    return "below average";
                else if (popularity <= 60)
                    return "moderately popular";
                else if (popularity <= 80)
                    return "popular";
                else
                    return "very popular";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CategorizePopularity for Popularity: {Popularity}", popularity);
                throw;
            }
        }

        public string CategorizeAlbumPopularity(int popularity)
        {
            try
            {
                if (popularity <= 20)
                    return "unpopular";
                else if (popularity <= 40)
                    return "below average";
                else if (popularity <= 60)
                    return "moderately popular";
                else if (popularity <= 80)
                    return "popular";
                else
                    return "very popular";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CategorizeAlbumPopularity for Popularity: {Popularity}", popularity);
                throw;
            }
        }

        private int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        // Calculate annual play count based on release date and precision
        private int? CalculateAnnualPlayCount(int totalPlayCount, string releaseDate, string releaseDatePrecision)
        {
            try
            {
                DateTime parsedReleaseDate;
                
                switch (releaseDatePrecision?.ToLower())
                {
                    case "day":
                        if (DateTime.TryParse(releaseDate, out parsedReleaseDate))
                            break;
                        return null;
                        
                    case "month":
                        if (DateTime.TryParse($"1 {releaseDate}", out parsedReleaseDate))
                            break;
                        return null;
                        
                    case "year":
                        if (DateTime.TryParse($"January 1, {releaseDate}", out parsedReleaseDate))
                            break;
                        return null;
                        
                    default:
                        // If we can't determine precision, try to parse the date as is
                        if (DateTime.TryParse(releaseDate, out parsedReleaseDate))
                            break;
                        return null;
                }
                
                double totalDays = (DateTime.UtcNow - parsedReleaseDate).TotalDays;
                
                // Ensure we don't divide by zero or negative days
                if (totalDays <= 0)
                    return totalPlayCount;
                    
                // Calculate plays per day, then multiply by days in a year
                double playsPerDay = totalPlayCount / totalDays;
                int annualPlayCount = (int)Math.Round(playsPerDay * 365.25);
                
                return annualPlayCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating annual play count for track with play count {PlayCount} and release date {ReleaseDate}", totalPlayCount, releaseDate);
                return null;
            }
        }
    }
}