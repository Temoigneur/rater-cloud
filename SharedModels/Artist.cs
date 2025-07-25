using Newtonsoft.Json;

namespace SharedModels.Artist
{
    // Root response class for artist data
    public class SpotifyArtistApiResponse
    {
        public SpotifyArtistData Data { get; set; }
    }

    public class SpotifyArtist
    {
        



        public string Id { get; set; }
        
        public string Name { get; set; }
    }

    public class SpotifyArtistData
    {
        public ArtistUnion ArtistUnion { get; set; }
    }

    public class ArtistUnion
    {
        public ArtistStats Stats { get; set; }
        public ArtistProfile Profile { get; set; }
    }

    public class ArtistStats
    {
        // MonthlyListeners is returned as an int? from the API
        public int? MonthlyListeners { get; set; }
    }

    public class ArtistProfile
    {
        public string Name { get; set; }
    }

    public class Track
    {
        public string Name { get; set; }
        // Updated: Spotify API returns a list of artists
        public List<Artist> Artists { get; set; }
    }

    public class Artist
    {
        public string Id { get; set; }  // Added property
        public string Name { get; set; }
        // Other properties...
    }
}


