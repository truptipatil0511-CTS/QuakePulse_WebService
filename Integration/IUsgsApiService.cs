using QuakePulse_WebService.Models.Internal;

namespace QuakePulse_WebService.Integration
{
    public interface IUsgsApiService
    {
        Task<string> GetEarthquakeDataAsync(EarthquakeQuery query);
        Task<string> GetEarthquakeByIdAsync(string id);
    }
}
