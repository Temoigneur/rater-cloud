using SharedModels.Common;
using System.Collections.Generic;

namespace SharedModels.Track
{
    public class OutputResponse
    {
        public string? OutputType { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        // Replace ArtistName with a list of artists
        public string? ArtistName { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumID { get; set; }

        public List<ArtistInfo>? Artists { get; set; }
        // Replace AlbumName and AlbumID with a nested Album object
        public AlbumInfo? Album { get; set; }
        public string? ReleaseDate { get; set; }
        public string? ReleaseDatePrecision { get; set; }
        public int? Popularity { get; set; }
        public string? PopularityRating { get; set; }
        public bool IsExplicit { get; set; }
        public bool IsCover { get; set; }
        public bool IsRemix { get; set; }
        public string? Genre { get; set; }
        
        // Play count properties
        public int? PlayCount { get; set; }
        public int? AnnualPlayCount { get; set; }
    }

    public class AlbumDetails
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumID { get; set; }
        public string ReleaseDatePrecision { get; set; }
        public Album Album { get; set; }
        public List<Artist> Artists { get; set; }
        public string? ReleaseDate { get; set; }
        public int Popularity { get; set; }
        public string PopularityRating { get; set; }
    }

    public class Album
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Artist
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
