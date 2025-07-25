using Newtonsoft.Json;


namespace SharedModels.Response
{
    public class ApifyResponse
    {
        [JsonProperty("data")]
        public ApifyData Data { get; set; }
    }

    public class ApifyRunResponse
    {
        [JsonProperty("data")]
        public ApifyRunData? Data { get; set; }
    }

    public class ApifyRunData
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }
    }

    public class ApifyRunStatusResponse
    {
        [JsonProperty("data")]
        public ApifyRunStatus? Data { get; set; }
    }

    public class ApifyRunStatus
    {
        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("statusMessage")]
        public string? StatusMessage { get; set; }
    }

    public class ApifyData
    {
        [JsonProperty("items")]
        public List<ApifyItem> Items { get; set; }
    }

    public class ApifyItem
    {
        [JsonProperty("url")]
        public string Url { get; set; } = "";

        [JsonProperty("streamCount")]
        public int StreamCount { get; set; } // Use "streamCount" property for playcount data
    }

}
