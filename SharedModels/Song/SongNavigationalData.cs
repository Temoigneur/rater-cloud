namespace SharedModels.Song
{
    public class SongNavigationalData
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumID { get; set; }
        public string? ReleaseDate { get; set; }
		public string? ReleaseDatePrecise { get; set; }
        public string? OutputType { get; set; }
        public string? IntentType { get; set; }
        public double? Popularity { get; set; }
        public string? PopularityRating { get; set; }
        public string? AlbumTracks { get; set; }
        public int? PlayCount { get; set; }
        public int? AnnualPlayCount { get; set; }
    }
    public class TrackData
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? ArtistName { get; set; }
    }
    public class AlbumData
    {
        public string? AlbumID { get; set; }
        public string? AlbumName { get; set; }
    }
    public class AlbumTrack
    {
        public string Name { get; set; }
        public string ArtistName { get; set; }
    }
    public class Artist
    {
        public string Name { get; set; }
        // Other properties...
    }
}
