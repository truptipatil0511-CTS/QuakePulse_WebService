using QuakePulse_WebService.Models.Internal;

namespace QuakePulse_WebService.Models.Api
{
    public class AssistantRequest
    {
        public string Query { get; set; } = string.Empty;
    }

    public class AssistantResponse
    {
        public Dictionary<string, object?> ParsedFilters { get; set; } = new();
        public List<EarthquakeDto> Data { get; set; } = new();
    }
}
