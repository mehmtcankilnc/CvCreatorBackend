using AutoMapper;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;

namespace CvCreator.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Resume, ResumeResponseDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.AppUserId));
        CreateMap<Resume, FileResponseDto>();
        CreateMap<CoverLetter, CoverLetterResponseDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.AppUserId));
        CreateMap<CoverLetter, FileResponseDto>();
    }
}
