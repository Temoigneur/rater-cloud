using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Response
{
    /// <summary>
    /// SpotScraper API response model
    /// </summary>
    public class SpotScraperResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("data")]
        public SpotScraperTrackData? Data { get; set; }
    }

    public class SpotScraperTrackData
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("statistics")]
        public SpotScraperStatistics? Statistics { get; set; }
    }

    public class SpotScraperStatistics
    {
        [JsonProperty("playCount")]
        public long? PlayCount { get; set; } // Changed from int? to long? to handle large play counts
    }
}
