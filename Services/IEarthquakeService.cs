using QuakePulse_WebService.Models.Api;
using QuakePulse_WebService.Models.Internal;

namespace QuakePulse_WebService.Services
{
    public interface IEarthquakeService
    {
        Task<EarthquakeListResponse> GetEarthquakesAsync(EarthquakeQuery query);
        Task<EarthquakeDto?> GetEarthquakeByIdAsync(string id);
        Task<EarthquakeListResponse> GetLatestAsync(int limit);
        Task<GeoJsonResponse> GetMapDataAsync(EarthquakeQuery query);
        Task<HtmlResponse> GetTableAsync(EarthquakeQuery query);
        Task<HtmlResponse> TransformToHtmlAsync(TransformRequest request);
        Task<AssistantResponse> QueryAssistantAsync(AssistantRequest request);
    }
}
