namespace SharedModels.Song
{
    public class SongFunctionalIntentResponse : SongFunctionalData
    {
        public string Intent { get; set; }
        public string IntentType { get; set; }
        public string Query { get; set; }
        public new string FormattedPlayCount { get; set; }
        public new string FormattedAnnualPlayCount { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? ArtistName { get; set; }
        public string? ArtistID { get; set; }
        public long? MonthlyListeners { get; set; }
        public string? ArtistPopularityRating { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumID { get; set; }
        public string? ReleaseDate { get; set; }
		public string? ReleaseDatePrecise { get; set; }
        public string? OutputType { get; set; }
        public long? PlayCount { get; set; }
        public double? PopularityScore { get; set; }
        public string? PopularityRating { get; set; }
        public string? AlbumTracks { get; set; }
    }
}
