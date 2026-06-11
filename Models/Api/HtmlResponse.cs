namespace QuakePulse_WebService.Models.Api
{
    public class HtmlResponse
    {
        public string Html { get; set; } = string.Empty;
        public string Source { get; set; } = "rule-based";  // "rule-based" | "genai" | "cache"
    }
}
