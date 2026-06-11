namespace QuakePulse_WebService.Models.Api
{
    public class GeoJsonResponse
    {
        public string Type { get; set; } = "FeatureCollection";
        public List<GeoJsonFeature> Features { get; set; } = new();
    }

    public class GeoJsonFeature
    {
        public string Type { get; set; } = "Feature";
        public GeoJsonGeometry Geometry { get; set; } = new();
        public Dictionary<string, object?> Properties { get; set; } = new();
    }

    public class GeoJsonGeometry
    {
        public string Type { get; set; } = "Point";
        public double[] Coordinates { get; set; } = Array.Empty<double>();
    }
}
