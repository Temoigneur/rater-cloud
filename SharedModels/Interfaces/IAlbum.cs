namespace SharedModels.Interfaces
{
    public interface IAlbum
    {
        public string? IntentType { get; set; }
        public string? Intent { get; set; }
        public string? Name { get; set; }
        public string? AlbumName { get; set; }
        public string? ArtistName { get; set; }
        public List<SpotifyArtist> Artists { get; set; }
        public string? ReleaseDate { get; set; }
        public int? Popularity { get; set; }
        public string? PopularityRating { get; set; }
        public string? Id { get; set; }
        public bool? IsExplicit { get; set; }
        public bool? IsCover { get; set; }
        public bool? IsRemix { get; set; }

    }
}
