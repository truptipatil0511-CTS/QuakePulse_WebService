using QuakePulse_WebService.Models.External;
using QuakePulse_WebService.Models.Internal;
using System.Text.Json;

namespace QuakePulse_WebService.Mappers
{
    public class EarthquakeMapper
    {

        public static EarthquakeResponseDto Map(string json)
        {
            var geoData = JsonSerializer.Deserialize<GeoJsonRoot>(json);

            var response = new EarthquakeResponseDto();

            if (geoData?.features == null)
                return response;

            foreach (var feature in geoData.features)
            {
                var dto = new EarthquakeDto
                {
                    Magnitude = feature.properties.mag,
                    Location = feature.properties.place,

                    Time = DateTimeOffset
                        .FromUnixTimeMilliseconds(feature.properties.time)
                        .DateTime,

                    Longitude = feature.geometry.coordinates[0],
                    Latitude = feature.geometry.coordinates[1],
                    Depth = feature.geometry.coordinates.Count > 2
                        ? feature.geometry.coordinates[2]
                        : 0,

                    Status = feature.properties.status,
                    Title = feature.properties.title,
                    Url = feature.properties.url
                };

                response.Earthquakes.Add(dto);
            }

            return response;
        }

    }
}
