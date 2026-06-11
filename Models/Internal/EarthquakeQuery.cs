namespace QuakePulse_WebService.Models.Internal
{
    public class EarthquakeQuery
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double? MinMagnitude { get; set; }
        public double? MaxMagnitude { get; set; }
        public string? Location { get; set; }
        public string? SortBy { get; set; }   // "magnitude" | "date"
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
        public double? Radius { get; set; }   // km
        public double? MinDepth { get; set; }
        public double? MaxDepth { get; set; }
    }
}
