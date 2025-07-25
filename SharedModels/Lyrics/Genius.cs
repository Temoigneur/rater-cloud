namespace SharedModels.Lyrics
{
    public class GeniusSearchResponse
    {
        public Meta meta { get; set; }
        public Response Response { get; set; }
    }
    public class Meta
    {
        public int status { get; set; }
    }

    public class Response
    {
        public List<Hit> Hits { get; set; }
    }

    public class Hit
    {
        public List<object> highlights { get; set; }
        public string index { get; set; }
        public string type { get; set; }
        public Result result { get; set; }
    }

    public class Result
    {
        public int annotation_count { get; set; }
        public string api_path { get; set; }
        public string artist_names { get; set; }
        public string full_title { get; set; }
        public string title { get; set; }
        public string header_image_thumbnail_url { get; set; }
        public string header_image_url { get; set; }
        public int id { get; set; }
        public int lyrics_owner_id { get; set; }
        public string lyrics_state { get; set; }
        public string path { get; set; }
        public string primary_artist_names { get; set; }
        public Stats stats { get; set; }
    }

    public class Stats
    {
        public int unreviewed_annotations { get; set; }
        public bool hot { get; set; }
        public int pageviews { get; set; }
    }

    public class Artist
    {
        public string? Name { get; set; }
    }

    public class GeniusTrack
    {
        public string? Name { get; set; }
        public string? ArtistName { get; set; }
    }
}
