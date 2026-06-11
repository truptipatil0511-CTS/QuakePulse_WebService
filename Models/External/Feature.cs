namespace QuakePulse_WebService.Models.External
{
    public class Feature
    {

        public string type { get; set; }
        public Properties properties { get; set; }
        public Geometry geometry { get; set; }
        public string id { get; set; }

    }
    public class Properties
    {
        public double? mag { get; set; }
        public string place { get; set; }
        public long time { get; set; }
        public long updated { get; set; }

        public string url { get; set; }
        public string detail { get; set; }

        public string status { get; set; }
        public int tsunami { get; set; }

        public double? sig { get; set; }

        public string magType { get; set; }
        public string type { get; set; }

        public string title { get; set; }
    }

    public class Geometry
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
    }

}
