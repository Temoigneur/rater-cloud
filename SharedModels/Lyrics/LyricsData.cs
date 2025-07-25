namespace SharedModels.Lyrics
{
    public class LyricsRequest
    {
        public string? Query { get; set; }
        public string? Classification { get; set; }
        public string? OutputText { get; set; }
    }

    public class TrackDetails
    {
        public string? Id { get; set; }
        public string? TrackId { get; set; }
        public string? Name { get; set; }
        public string? ArtistName { get; set; }
        public DateTime? ReleaseDateRaw { get; set; }
        public string? ReleaseDate { get; set; }

        public int? Popularity { get; set; }
        public string? PopularityRating { get; set; }
        public bool IsExplicit { get; set; }
        public bool IsCover { get; set; }
        public bool IsRemix { get; set; }
    }

    public class EvaluationResult
    {
        public object OpenAIResult { get; set; }
        public object PerplexityResult { get; set; }
    }

    public class LyricsTrackEvaluation
    {
        //public string? LyricsMatchMarker { get; set; }
        public string Id { get; set; }
        public string ArtistName { get; set; }
        public string Name { get; set; }
        public long PlayCount { get; set; } // Changed from int to long
        public long AnnualPlayCount { get; set; } // Changed from int to long
        public string FormattedPlayCount { get; set; }
        public string FormattedAnnualPlayCount { get; set; }
        public DateTime? ReleaseDateRaw { get; set; }
        public string ReleaseDate { get; set; }
        public bool IsExplicit { get; set; }
        public bool IsCover { get; set; }
        public bool IsRemix { get; set; }
        // Add other properties as needed
        // New context properties
        public string Query { get; set; }
        public string Classification { get; set; }
        public string Output { get; set; }
        public string OutputType { get; set; }
    }
    public class LyricsSearchRequest
    {
        public string Query { get; set; }
        public string Output { get; set; }
        public string Classification { get; set; }
        public string OutputType { get; set; }
    }
}