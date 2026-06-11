using QuakePulse_WebService.Models.Internal;

namespace QuakePulse_WebService.Models.Api
{
    public class EarthquakeListResponse
    {
        public ResponseMetadata Metadata { get; set; } = new();
        public List<EarthquakeDto> Data { get; set; } = new();
    }

    public class ResponseMetadata
    {
        public int Count { get; set; }
        public string Source { get; set; } = "USGS";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Cached { get; set; }
    }
}
