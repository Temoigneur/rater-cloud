using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedModels.Track;

namespace SharedModels.Common
{
    class SpotifyModels
    {
    }



    public class AlbumInfo
    {
        // Explicit conversion operator from AlbumDetails
        public static explicit operator AlbumInfo(AlbumDetails albumDetails)
        {
            if (albumDetails == null) return null;

            return new AlbumInfo
            {
                Id = albumDetails.Id,
                Name = albumDetails.Name,
                ReleaseDate = albumDetails.ReleaseDate,
                ReleaseDatePrecision = albumDetails.ReleaseDatePrecision
                // Add other properties as needed
            };
        }

        public string? Id { get; set; }
        public string? Name { get; set; }
        // Spotify returns releaseDate and release_date_precision on albums:
        public string? ReleaseDate { get; set; }
        public string? ReleaseDatePrecision { get; set; }
        public int? Popularity { get; set; }
        // List of artists on the album
        public List<ArtistInfo>? Artists { get; set; }

    }

    public class ArtistInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}
