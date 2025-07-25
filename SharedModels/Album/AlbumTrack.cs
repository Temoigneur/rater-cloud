using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Album
{
    public class AlbumTrack
    {
        /// <summary>
        /// The Spotify ID of the track.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the track.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The track number within the album.
        /// </summary>
        public int TrackNumber { get; set; }

        /// <summary>
        /// The disc number within the album.
        /// </summary>
        public int DiscNumber { get; set; }

        /// <summary>
        /// The duration of the track in milliseconds.
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// Whether the track is explicit.
        /// </summary>
        public bool IsExplicit { get; set; }

        /// <summary>
        /// The artists who performed the track.
        /// </summary>
        public List<string> ArtistNames { get; set; }

        /// <summary>
        /// The popularity of the track (0-100).
        /// </summary>
        public int? Popularity { get; set; }
    }
}
