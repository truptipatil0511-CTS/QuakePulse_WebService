namespace QuakePulse_WebService.Models.Internal
{
    public class EarthquakeDto
    {
        public string? Id { get; set; }
        public double? Magnitude { get; set; }
        public string Location { get; set; }

        public DateTime Time { get; set; }

        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Depth { get; set; }

        public string Status { get; set; }
        public string Title { get; set; }

        public string Url { get; set; }

    }

    public class EarthquakeResponseDto
    {
        public List<EarthquakeDto> Earthquakes { get; set; } = new();
    }

}
