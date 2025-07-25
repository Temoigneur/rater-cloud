using SpotifyAPI.Web;
using SpotifyPrivate;
using SharedModels.Album;
using SharedModels.Track;
using SharedModels.Utilities;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Net;
using SpotifyAPI.Web.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Rater.Services
{
    public class SpotifyService : ISpotifyService
    {
        // Public (SpotifyAPI.Web) client
        private SpotifyClient _spotifyWebClient;

        // Private (SpotifyPrivate.API) client
        private SpotifyPrivate.API _spotifyAPIClient;

        private readonly IConfiguration _configuration;
        private readonly ILogger<SpotifyService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;

        // Internal initialization flag
        private bool _initialized = false;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        public SpotifyService(
            IConfiguration configuration,
            ILogger<SpotifyService> logger,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            // No async work here; just store references
        }

        /// <summary>
        /// The one-time async initializer for both SpotifyAPI.Web and SpotifyPrivate.API.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_initialized)
                    return;

                // 1) Initialize the PUBLIC Spotify client
                var httpClient = _httpClientFactory.CreateClient("SpotifyWeb");
                var webConfig = SpotifyClientConfig
                    .CreateDefault()
                    .WithHTTPClient(new NetHttpClient(httpClient));

                var clientId = _configuration["Spotify:ClientId"];
                var clientSecret = _configuration["Spotify:ClientSecret"];

                var request = new ClientCredentialsRequest(clientId, clientSecret);
                var response = await new OAuthClient(webConfig).RequestToken(request);

                _spotifyWebClient = new SpotifyClient(
                    webConfig.WithToken(response.AccessToken)
                );

                // 2) Initialize the PRIVATE Spotify client
                var privateApiKey = _configuration["Spotify:PrivateApiKey"];
                // If you have a proxy, set it up here. Otherwise pass null.
                _spotifyAPIClient = new API(token: privateApiKey, proxyConfig: null);

                _initialized = true;
                _logger.LogInformation("SpotifyService successfully initialized with both API clients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing SpotifyService");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        /// <summary>
        /// Ensures the service is initialized before using any Spotify API calls.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Check if the service is healthy (i.e., successfully initialized).
        /// </summary>
        public bool IsServiceHealthy()
        {
            // We do NOT force init in this method to avoid sync over async;
            // just report the _initialized status.
            return _initialized;
        }

        /// <summary>
        /// Force re-init if you want fresh tokens, etc.
        /// Callers should await this if they want guaranteed readiness.
        /// </summary>
        public async Task Reinitialize()
        {
            _initialized = false;
            await InitializeAsync();
        }

        // -------------------------------------------------------------------------
        // PUBLIC SPOTIFY (SpotifyAPI.Web) examples
        // -------------------------------------------------------------------------

        public async Task<SearchResponse> SearchWebApiAsync(string query)
        {
            await EnsureInitializedAsync();
            return await _spotifyWebClient.Search.Item(
                new SearchRequest(SearchRequest.Types.Track, query)
            );
        }

        public async Task<object> SearchTracksAsync(string query)
        {
            await EnsureInitializedAsync();
            return await _spotifyWebClient.Search.Item(
                new SearchRequest(SearchRequest.Types.Track, query)
            );
        }

        public async Task<SharedModels.Album.AlbumResponse> SearchAlbumAsync(string albumName, string artistName)
        {
            try
            {
                await EnsureInitializedAsync();

                string cacheKey = $"album_search_{albumName}_{artistName}";
                if (_cache.TryGetValue(cacheKey, out SharedModels.Album.AlbumResponse cachedResponse))
                {
                    return cachedResponse;
                }

                var query = string.IsNullOrWhiteSpace(artistName) ? albumName : $"{albumName} {artistName}";
                var searchRequest = new SearchRequest(SearchRequest.Types.Album, query) { Limit = 1 };

                var searchResponse = await _spotifyWebClient.Search.Item(searchRequest);
                if (searchResponse.Albums?.Items?.Count > 0)
                {
                    var album = searchResponse.Albums.Items[0];
                    var response = new SharedModels.Album.AlbumResponse
                    {
                        AlbumID = album.Id,
                        AlbumName = album.Name,
                        ArtistName = string.Join(", ", album.Artists.Select(a => a.Name))
                    };

                    _cache.Set(cacheKey, response, TimeSpan.FromHours(1));
                    return response;
                }

                _logger.LogWarning("No albums found for query: {Query}", query);
                return CreateMockAlbumResponse(albumName, artistName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchAlbumAsync for query: {0} {1}", albumName, artistName);
                return CreateMockAlbumResponse(albumName, artistName);
            }
        }

        private SharedModels.Album.AlbumResponse CreateMockAlbumResponse(string albumName, string artistName)
        {
            return new SharedModels.Album.AlbumResponse
            {
                AlbumID = $"mock_album_{Guid.NewGuid()}",
                AlbumName = albumName ?? "Unknown Album",
                ArtistName = artistName ?? "Unknown Artist"
            };
        }

        // -------------------------------------------------------------------------
        // PRIVATE SPOTIFY (SpotifyPrivate.API) examples
        // -------------------------------------------------------------------------

        public async Task<object> GetPrivateTrackDataAsync(string id)
        {
            await EnsureInitializedAsync();
            return await _spotifyAPIClient.GetTrack(id);
        }

        public async Task<PrivateSearchResults> SearchPrivateApiAsync(string trackName, string artistName)
        {
            await EnsureInitializedAsync();

            // Your SpotifyPrivate.API code doesn't show a direct "SearchTracksAsync" method,
            // but here's an example of returning a custom result object:
            return new PrivateSearchResults
            {
                Query = $"{trackName} {artistName}",
                Items = new List<string> { "Track1", "Track2" }
            };
        }

        public class PrivateSearchResults
        {
            public string Query { get; set; }
            public List<string> Items { get; set; }
        }

        // -------------------------------------------------------------------------
        // Combined logic examples: get artist details using both public & private
        // -------------------------------------------------------------------------

        public async Task<(string ArtistID, long MonthlyListeners)?> GetArtistDetailsAsync(string artistName)
        {
            try
            {
                await EnsureInitializedAsync();

                // Search for artist using public API
                var artistSearch = await _spotifyWebClient.Search.Item(
                    new SearchRequest(SearchRequest.Types.Artist, artistName) { Limit = 1 }
                );
                var artistItem = artistSearch.Artists?.Items?.FirstOrDefault(
                    a => a.Name.Equals(artistName, StringComparison.OrdinalIgnoreCase)
                );
                if (artistItem == null)
                    return null;

                string artistID = artistItem.Id;

                // Attempt private API for monthly listeners
                try
                {
                    var artistData = await _spotifyAPIClient.GetArtist(artistID);
                    if (artistData?.Data?.ArtistUnion?.Stats?.MonthlyListeners.HasValue == true)
                    {
                        return (artistID, (long)artistData.Data.ArtistUnion.Stats.MonthlyListeners.Value);
                    }
                }
                catch (Exception exPrivate)
                {
                    _logger.LogError(exPrivate, "Error in private API for artist {ArtistName}", artistName);
                }

                // Fallback if private data missing
                return (artistID, artistItem.Popularity * 10000L);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetArtistDetailsAsync({ArtistName})", artistName);
                return ("mock_artist_id", 1_000_000L);
            }
        }

        public string CategorizePopularity(long playCount)
        {
            if (playCount > 10_000_000) return "Very Popular";
            if (playCount > 1_000_000) return "Moderately Popular";
            return "Unpopular";
        }

        public string CategorizeAlbumPopularity(int popularityScore)
        {
            if (popularityScore >= 80) return "Very Popular";
            if (popularityScore >= 50) return "Moderately Popular";
            return "Unpopular";
        }

        public double GetPopularityRating(int popularityScore)
        {
            if (popularityScore >= 80) return 5.0;
            if (popularityScore >= 50) return 3.0;
            return 1.0;
        }

        // -------------------------------------------------------------------------
        // Searching tracks (public) then returning minimal info
        // -------------------------------------------------------------------------

        public async Task<TrackResponse> SearchTrackAsync(string trackName, string artistName)
        {
            try
            {
                await EnsureInitializedAsync();

                var query = string.IsNullOrWhiteSpace(artistName)
                    ? trackName
                    : $"{trackName} {artistName}";

                var searchRequest = new SearchRequest(SearchRequest.Types.Track, query) { Limit = 1 };
                var searchResponse = await _spotifyWebClient.Search.Item(searchRequest);

                if (searchResponse.Tracks?.Items?.Count > 0)
                {
                    var track = searchResponse.Tracks.Items[0];
                    return new TrackResponse
                    {
                        TrackID = track.Id,
                        TrackName = track.Name,
                        ArtistName = string.Join(", ", track.Artists.Select(a => a.Name))
                    };
                }

                _logger.LogWarning("No tracks found for query: {Query}", query);
                return CreateMockTrackResponse(trackName, artistName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchTrackAsync({0}, {1})", trackName, artistName);
                return CreateMockTrackResponse(trackName, artistName);
            }
        }

        private TrackResponse CreateMockTrackResponse(string trackName, string artistName)
        {
            return new TrackResponse
            {
                TrackID = $"mock_{Guid.NewGuid()}",
                TrackName = trackName,
                ArtistName = artistName ?? "Unknown Artist"
            };
        }

        // -------------------------------------------------------------------------
        // Detailed track: private API first, fallback to public
        // -------------------------------------------------------------------------

        public async Task<SharedModels.Track.TrackDetails> GetTrackDetailsAsync(string trackID)
        {
            try
            {
                await EnsureInitializedAsync();

                // If it's a mock ID
                if (trackID.StartsWith("mock_"))
                {
                    return CreateMockTrackDetails(trackID);
                }

                // Try cache
                string cacheKey = $"track_details_{trackID}";
                if (_cache.TryGetValue(cacheKey, out SharedModels.Track.TrackDetails cachedDetails))
                {
                    return cachedDetails;
                }

                // 1) Attempt private API
                try
                {
                    var trackData = await _spotifyAPIClient.GetTrack(trackID);
                    if (trackData?.Data?.TrackUnion == null)
                    {
                        _logger.LogWarning("Private API returned no track data for {TrackID}", trackID);
                        return CreateMockTrackDetails(trackID);
                    }

                    var trackUnion = trackData.Data.TrackUnion;
                    long playCount = 0;
                    long.TryParse(trackUnion.Playcount, out playCount);

                    DateTime? parsedDate = null;
                    if (DateTime.TryParse(trackUnion.AlbumOfTrack?.Date?.IsoString, out var tempDate))
                    {
                        parsedDate = tempDate;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse private API date for track {TrackID}", trackID);
                    }

                    var details = new SharedModels.Track.TrackDetails
                    {
                        TrackID = trackID,
                        TrackName = trackUnion.Name,
                        ArtistName = trackUnion.FirstArtist?.Items?.FirstOrDefault()?.Profile?.Name ?? "Unknown Artist",
                        AlbumName = trackUnion.AlbumOfTrack?.Name ?? "Unknown Album",
                        AlbumID = trackUnion.AlbumOfTrack?.Uri?.Split(':').LastOrDefault(),
                        PlayCount = playCount,
                        ReleaseDateRaw = parsedDate,
                        ReleaseDate = MappingHelper.FormatReleaseDate(parsedDate),
                        FormattedPlayCount = MappingHelper.FormatPlayCount(playCount),
                        AnnualPlayCount = MappingHelper.CalculateAnnualPlayCount(playCount, parsedDate),
                        FormattedAnnualPlayCount = MappingHelper.FormatPlayCount(
                            (long)MappingHelper.CalculateAnnualPlayCount(playCount, parsedDate)
                        ),
                        PopularityRating = MappingHelper.CategorizePopularity(playCount),
                        IsExplicit = trackUnion.ContentRating?.Label == "explicit",
                        IsCover = false,
                        IsRemix = false
                    };

                    _cache.Set(cacheKey, details, TimeSpan.FromHours(1));
                    return details;
                }
                catch (Exception exPrivate)
                {
                    _logger.LogError(exPrivate, "Error fetching track from private API for {TrackID}", trackID);

                    // 2) Fallback to public
                    try
                    {
                        var publicTrack = await _spotifyWebClient.Tracks.Get(trackID);
                        var details = new SharedModels.Track.TrackDetails
                        {
                            TrackID = trackID,
                            TrackName = publicTrack.Name,
                            ArtistName = string.Join(", ", publicTrack.Artists.Select(a => a.Name)),
                            AlbumName = publicTrack.Album.Name,
                            AlbumID = publicTrack.Album.Id,
                            PlayCount = publicTrack.Popularity * 10000, // estimate
                            ReleaseDateRaw = DateTime.TryParse(publicTrack.Album.ReleaseDate, out var date)
                                ? date
                                : (DateTime?)null,
                            ReleaseDate = publicTrack.Album.ReleaseDate,
                            FormattedPlayCount = MappingHelper.FormatPlayCount(publicTrack.Popularity * 10000),
                            PopularityRating = MappingHelper.CategorizePopularity(publicTrack.Popularity * 10000),
                            IsExplicit = publicTrack.Explicit,
                            IsCover = false,
                            IsRemix = false
                        };

                        if (details.ReleaseDateRaw.HasValue)
                        {
                            details.AnnualPlayCount = MappingHelper.CalculateAnnualPlayCount(
                                details.PlayCount,
                                details.ReleaseDateRaw
                            );
                            details.FormattedAnnualPlayCount = MappingHelper.FormatPlayCount((long)details.AnnualPlayCount);
                        }

                        _cache.Set(cacheKey, details, TimeSpan.FromHours(1));
                        return details;
                    }
                    catch (Exception publicApiEx)
                    {
                        _logger.LogError(publicApiEx, "Error fetching track from public API for {TrackID}", trackID);
                        return CreateMockTrackDetails(trackID);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTrackDetailsAsync for {TrackID}", trackID);
                return CreateMockTrackDetails(trackID);
            }
        }

        private SharedModels.Track.TrackDetails CreateMockTrackDetails(string trackID)
        {
            string trackName = "Unknown Track";
            string artistName = "Unknown Artist";

            if (trackID.StartsWith("mock_"))
            {
                var parts = trackID.Split('_');
                if (parts.Length > 2)
                {
                    trackName = parts[1];
                    if (parts.Length > 3)
                        artistName = parts[2];
                }
            }

            var releaseDate = DateTime.Now.AddYears(-1);

            return new SharedModels.Track.TrackDetails
            {
                TrackID = trackID,
                TrackName = trackName,
                ArtistName = artistName,
                AlbumName = "Mock Album",
                AlbumID = $"mock_album_{Guid.NewGuid()}",
                PlayCount = 1_000_000,
                ReleaseDateRaw = releaseDate,
                ReleaseDate = MappingHelper.FormatReleaseDate(releaseDate),
                FormattedPlayCount = "1M",
                AnnualPlayCount = 1_000_000,
                FormattedAnnualPlayCount = "1M",
                PopularityRating = "Moderately Popular",
                IsExplicit = false,
                IsCover = false,
                IsRemix = false
            };
        }

        public async Task<TrackDetails> GetCachedTrackDetailsAsync(string trackID)
        {
            return await _cache.GetOrCreateAsync(trackID, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await GetTrackDetailsAsync(trackID);
            });
        }

        // -------------------------------------------------------------------------
        // Searching for an album and getting details
        // -------------------------------------------------------------------------

        public async Task<SharedModels.Album.AlbumResponse> SearchAlbumAsync(string albumName, string artistName, bool redundantOverload)
        {
            // Example overload usage if you need it; or you can remove if not used
            return await SearchAlbumAsync(albumName, artistName);
        }

        public async Task<SharedModels.Album.AlbumDetails> GetAlbumDetailsAsync(string albumID)
        {
            try
            {
                await EnsureInitializedAsync();

                if (albumID.StartsWith("mock_"))
                {
                    return CreateMockAlbumDetails(albumID);
                }

                string cacheKey = $"album_details_{albumID}";
                if (_cache.TryGetValue(cacheKey, out SharedModels.Album.AlbumDetails cachedDetails))
                {
                    return cachedDetails;
                }

                try
                {
                    // Public album info
                    var album = await _spotifyWebClient.Albums.Get(albumID);
                    if (album == null)
                    {
                        _logger.LogWarning("Album not found for AlbumID: {0}", albumID);
                        return CreateMockAlbumDetails(albumID);
                    }

                    var artistName = (album.Artists != null && album.Artists.Any())
                        ? string.Join(", ", album.Artists.Select(a => a.Name))
                        : "Unknown Artist";

                    DateTime? releaseDate = null;

                    // Try private API for more accurate release date
                    try
                    {
                        var privateAlbumResponse = await _spotifyAPIClient.GetAlbum(albumID);
                        var isoString = privateAlbumResponse?.Data?.AlbumUnion?.Date?.IsoString;
                        // isoString is a DateTime?, so check for HasValue
                        if (isoString.HasValue)
                        {
                            releaseDate = isoString.Value;
                        }
                    }
                    catch (Exception privateApiEx)
                    {
                        _logger.LogError(privateApiEx, "Error in private API for album {0}", albumID);

                        // fallback: parse from public release date
                        if (!string.IsNullOrEmpty(album.ReleaseDate) &&
                            DateTime.TryParse(album.ReleaseDate, out var parsedPubDate))
                        {
                            releaseDate = parsedPubDate;
                        }
                    }

                    var details = new SharedModels.Album.AlbumDetails
                    {
                        AlbumID = album.Id,
                        AlbumName = album.Name,
                        ArtistName = artistName,
                        ReleaseDateRaw = releaseDate,
                        ReleaseDateFormatted = MappingHelper.FormatReleaseDate(releaseDate),
                        PopularityScore = album.Popularity,
                        PopularityRating = MappingHelper.CategorizeAlbumPopularity(album.Popularity)
                    };

                    _cache.Set(cacheKey, details, TimeSpan.FromHours(1));
                    return details;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in GetAlbumDetailsAsync for {0}", albumID);
                    return CreateMockAlbumDetails(albumID);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAlbumDetailsAsync for {0}", albumID);
                return CreateMockAlbumDetails(albumID);
            }
        }

        private SharedModels.Album.AlbumDetails CreateMockAlbumDetails(string albumID)
        {
            string albumName = "Unknown Album";
            string artistName = "Unknown Artist";

            if (albumID.StartsWith("mock_album_"))
            {
                var parts = albumID.Split('_');
                if (parts.Length > 3)
                {
                    albumName = parts[2];
                    artistName = parts.Length > 4 ? parts[3] : "Unknown Artist";
                }
            }

            var releaseDate = DateTime.Now.AddYears(-1);
            return new SharedModels.Album.AlbumDetails
            {
                AlbumID = albumID,
                AlbumName = albumName,
                ArtistName = artistName,
                ReleaseDateRaw = releaseDate,
                ReleaseDateFormatted = MappingHelper.FormatReleaseDate(releaseDate),
                PopularityScore = 65,
                PopularityRating = "Moderately Popular",
            };
        }

        // -------------------------------------------------------------------------
        // For demonstration: manually re-check initialization if needed
        // -------------------------------------------------------------------------
        public bool IsServiceHealthyDeprecated()
        {
            // Minimal alternative example to “IsServiceHealthy()”.
            // We do NOT call InitializeAsync() here to avoid sync blocking.
            return _initialized;
        }
    }
}
