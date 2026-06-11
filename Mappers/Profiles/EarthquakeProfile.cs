using AutoMapper;
using QuakePulse_WebService.Models.External;
using QuakePulse_WebService.Models.Internal;

namespace QuakePulse_WebService.Mappers.Profiles
{
    public class EarthquakeProfile: Profile
    {
        public EarthquakeProfile()
        {
            CreateMap<Feature, EarthquakeDto>()
                .ForMember(dest => dest.Id,
                    opt => opt.MapFrom(src => src.id))

                .ForMember(dest => dest.Magnitude,
                    opt => opt.MapFrom(src => src.properties.mag))

                .ForMember(dest => dest.Location,
                    opt => opt.MapFrom(src => src.properties.place))

                .ForMember(dest => dest.Time,
                    opt => opt.MapFrom(src =>
                        DateTimeOffset
                            .FromUnixTimeMilliseconds(src.properties.time)
                            .DateTime))

                .ForMember(dest => dest.Longitude,
                    opt => opt.MapFrom(src => src.geometry.coordinates[0]))

                .ForMember(dest => dest.Latitude,
                    opt => opt.MapFrom(src => src.geometry.coordinates[1]))

                .ForMember(dest => dest.Depth,
                    opt => opt.MapFrom(src =>
                        src.geometry.coordinates.Count > 2
                            ? src.geometry.coordinates[2]
                            : 0))

                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.properties.status))

                .ForMember(dest => dest.Title,
                    opt => opt.MapFrom(src => src.properties.title))

                .ForMember(dest => dest.Url,
                    opt => opt.MapFrom(src => src.properties.url));
        }
    }

}
