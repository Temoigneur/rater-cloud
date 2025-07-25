using System;

namespace SharedModels.Utilities
{
    /// <summary>
    /// Shared formatting utilities for consistent display of data across different projects
    /// </summary>
    public static class FormatUtilities
    {
        /// <summary>
        /// Formats a play count number into a human-readable string with K, M, B suffixes
        /// </summary>
        /// <param name="playCount">The play count to format</param>
        /// <returns>Formatted play count string</returns>
        public static string FormatPlayCount(int? playCount)
        {
            if (!playCount.HasValue || playCount.Value == 0)
                return "N/A";
                
            if (playCount.Value < 0)
                return "N/A";
                
            if (playCount.Value < 1000)
                return playCount.Value.ToString();
                
            if (playCount.Value < 1_000_000)
                return $"{playCount.Value / 1000.0:0.#}K";
                
            if (playCount.Value < 1_000_000_000)
                return $"{playCount.Value / 1_000_000.0:0.#}M";
                
            return $"{playCount.Value / 1_000_000_000.0:0.#}B";
        }
    }
}
