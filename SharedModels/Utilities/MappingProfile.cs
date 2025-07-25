using AutoMapper;
using SharedModels.Common;
using SharedModels.Track;
namespace SharedModels.Utilities
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<SharedModels.Album.AlbumDetails, OutputResponse>()
                .ForMember(dest => dest.OutputType, opt => opt.MapFrom(src => "album"))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AlbumID, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.AlbumName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate))
                .ForMember(dest => dest.Album, opt => opt.MapFrom(src => new AlbumInfo
                {
                    Id = src.Id,
                    Name = src.Name,
                    ReleaseDate = src.ReleaseDate,
                    ReleaseDatePrecision = src.ReleaseDatePrecision
                }));

            CreateMap<TrackDetails, OutputResponse>()
                .ForMember(dest => dest.OutputType, opt => opt.MapFrom(src => "track"))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.AlbumID, opt => opt.MapFrom(src => src.Album != null ? src.Album.Id : src.AlbumID))
                .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.Album != null ? src.Album.ReleaseDate : src.ReleaseDate))
                .ForMember(dest => dest.ReleaseDatePrecision, opt => opt.MapFrom(src => src.Album != null ? src.Album.ReleaseDatePrecision : src.ReleaseDatePrecision))
                .ForMember(dest => dest.Album, opt => opt.MapFrom(src => new AlbumInfo
                {
                    Id = src.Album != null ? src.Album.Id : src.AlbumID,
                    Name = src.AlbumName,
                    ReleaseDate = src.Album != null ? src.Album.ReleaseDate : src.ReleaseDate,
                    ReleaseDatePrecision = src.Album != null ? src.Album.ReleaseDatePrecision : src.ReleaseDatePrecision
                }));
        }
    }
}