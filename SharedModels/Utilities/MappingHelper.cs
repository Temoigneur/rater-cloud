using System.Globalization;
using SharedModels.Album;
using SharedModels.Request;
using SharedModels.Track;
using SpotifyAPI.Web;
using SharedModels;
using SharedModels.Common;

namespace SharedModels.Utilities
{
    public static class MappingHelper
    {
        public static string FormatReleaseDate(string releaseDate)
        {
            if (string.IsNullOrEmpty(releaseDate))
                return "Unknown";

            // Try to parse the date with various formats
            if (DateTime.TryParse(releaseDate, out var parsedDate))
            {
                return parsedDate.ToString("MM/yyyy", CultureInfo.InvariantCulture);
            }
            else if (int.TryParse(releaseDate, out var year) && year > 1000 && year < 3000)
            {
                // If it's just a year
                return $"01/{year}";
            }

            return releaseDate;
        }

        public static string? CategorizePopularity(int popularity)
        {
            if (popularity <= 20)
                return "unpopular";
            else if (popularity <= 40)
                return "below average";
            else if (popularity <= 60)
                return "moderately popular";
            else if (popularity <= 80)
                return "popular";
            else
                return "very popular";
        }

        public static OutputResponse MapToOutputResponse(SharedModels.Album.AlbumDetails albumDetails)
        {
            if (albumDetails == null) return null;

            return new OutputResponse
            {
                OutputType = "album",
                Id = albumDetails.Id,
                AlbumID = albumDetails.Id,
                Name = albumDetails.Name,
                AlbumName = albumDetails.Name,
                ArtistName = albumDetails.ArtistName,
                ReleaseDatePrecision = FormatReleaseDateLong(albumDetails.ReleaseDate),
                ReleaseDate = albumDetails.ReleaseDate,
                Popularity = albumDetails.Popularity,
                PopularityRating = CategorizePopularity((int)albumDetails.Popularity),
                PlayCount = albumDetails.PlayCount,
                AnnualPlayCount = albumDetails.AnnualPlayCount,
                Album = new AlbumInfo
                {
                    Id = albumDetails.Id,
                    Name = albumDetails.Name,
                    ReleaseDate = albumDetails.ReleaseDate,
                    ReleaseDatePrecision = FormatReleaseDateLong(albumDetails.ReleaseDate)
                }
            };
        }

        public static OutputResponse MapToOutputResponse(TrackDetails trackDetails)
        {
            if (trackDetails == null) return null;

            return new OutputResponse
            {
                OutputType = "track",
                Id = trackDetails.Id,
                Name = trackDetails.Name,
                ArtistName = trackDetails.ArtistName,
                AlbumName = trackDetails.AlbumName,
                // Include both Id and AlbumID for compatibility
                AlbumID = trackDetails.Album?.Id ?? trackDetails.AlbumID ?? string.Empty,
                Popularity = trackDetails.Popularity,
                ReleaseDate = trackDetails.ReleaseDate,
                ReleaseDatePrecision = trackDetails.ReleaseDatePrecision,
                PopularityRating = CategorizePopularity((int)(trackDetails.Popularity ?? 0)),
                IsExplicit = trackDetails.IsExplicit,
                IsCover = trackDetails.IsCover,
                IsRemix = trackDetails.IsRemix,
                PlayCount = trackDetails.PlayCount,
                AnnualPlayCount = trackDetails.AnnualPlayCount,
                Album = new AlbumInfo
                {
                    Id = trackDetails.Album?.Id ?? trackDetails.AlbumID ?? string.Empty,
                    Name = trackDetails.AlbumName ?? string.Empty,
                    ReleaseDate = trackDetails.Album?.ReleaseDate ?? trackDetails.ReleaseDate ?? string.Empty,
                    ReleaseDatePrecision = trackDetails.Album?.ReleaseDatePrecision ?? trackDetails.ReleaseDatePrecision ?? string.Empty
                }
            };
        }

        public static string FormatReleaseDateLong(string releaseDate)
        {
            if (string.IsNullOrEmpty(releaseDate))
                return "Unknown";

            // Try to parse the date with various formats
            if (DateTime.TryParse(releaseDate, out var parsedDate))
            {
                return parsedDate.ToString("MMMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));
            }
            else if (int.TryParse(releaseDate, out var year) && year > 1000 && year < 3000)
            {
                // If it's just a year
                return year.ToString();
            }

            return releaseDate;
        }
    }
}