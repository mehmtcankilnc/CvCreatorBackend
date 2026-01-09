using AutoMapper;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;

namespace CvCreator.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Resume, ResumeResponseDto>();
        CreateMap<Resume, FileResponseDto>();
        CreateMap<CoverLetter, CoverLetterResponseDto>();
        CreateMap<CoverLetter, FileResponseDto>();
    }
}
