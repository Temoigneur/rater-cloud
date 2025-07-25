// IApifyService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rater.Services
{
    public interface IApifyService
    {
        /// <summary>
        /// Retrieves play counts for the given list of Spotify track URLs
        /// </summary>
        /// <param name="trackUrls">List of Spotify track URLs</param>
        /// <returns>Dictionary mapping track URLs to their play counts</returns>
        Task<Dictionary<string, int?>> GetPlayCountsAsync(List<string> trackUrls);
        
        /// <summary>
        /// Clears the play count cache for a specific track or all tracks
        /// </summary>
        /// <param name="trackId">Track ID to clear, or null to clear all cache entries</param>
        Task ClearCacheAsync(string? trackId = null);
        
        /// <summary>
        /// Extracts the track ID from a Spotify track URL
        /// </summary>
        /// <param name="url">Spotify track URL</param>
        /// <returns>Track ID or null if extraction fails</returns>
        string? ExtractTrackIdFromUrl(string url);
    }
}
