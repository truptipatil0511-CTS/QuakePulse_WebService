using System.Text.Json;

namespace QuakePulse_WebService.Models.Api
{
    public class TransformRequest
    {
        public List<JsonElement> Data { get; set; } = new();
        public string Mode { get; set; } = "rule";   // "rule" | "genai"
    }
}
