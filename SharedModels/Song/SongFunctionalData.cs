namespace SharedModels.Song
{
    public class SongFunctionalData
    {
        public string? Name { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumName { get; set; }
        public string? ReleaseDate { get; set; }
        public string? ReleaseDatePrecise { get; set; }
        public string? OutputType { get; set; }
        public string? IntentType { get; set; }
        public string? Intent { get; set; }
        public double? Popularity { get; set; }
        public string? PopularityRating { get; set; }
        public string Query { get; set; }
        public int? PlayCount { get; set; }
        public int? AnnualPlayCount { get; set; }
    }
}
