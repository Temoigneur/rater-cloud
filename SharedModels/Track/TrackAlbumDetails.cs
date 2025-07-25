namespace SharedModels.Track
{
    public class TrackAlbumDetails
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? artists { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumID { get; set; }
        public long? PlayCount { get; set; }
        public double? AnnualPlayCount { get; set; }
        public string? FormattedPlayCount { get; set; }
        public string? FormattedAnnualPlayCount { get; set; }
        public int? Popularity { get; set; }
        public string? ReleaseDate { get; set; }
        public string? ReleaseDateRaw { get; set; }
        public string? ReleaseDatePrecision { get; set; }
        public string? PopularityRating { get; set; }
        public bool IsExplicit { get; set; }
        public bool IsCover { get; set; }
        public bool IsRemix { get; set; }
        public string? Genre { get; set; }
    }

}
