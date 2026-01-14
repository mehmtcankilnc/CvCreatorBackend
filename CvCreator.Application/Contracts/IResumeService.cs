using CvCreator.Application.DTOs;
using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface IResumeService
{
    Task<byte[]> CreateResumePdfAsync(ResumeFormValuesModel model, string templateName);
    Task<byte[]> GenerateResumeAsync(ResumeFormValuesModel model, string templateName, Guid? userId);
    Task SaveResumeAsync(byte[] fileContent, string userId, ResumeFormValuesModel model);
    Task<List<FileResponseDto>> GetResumesAsync(Guid id, string? searchText, int? limit);
    Task<ResumeResponseDto?> FindResumeByIdAsync(Guid resumeId);
    Task DeleteResumeAsync(ResumeResponseDto resume);
    Task UpdateResume(byte[] fileContent, Guid resumeId, ResumeFormValuesModel model);
    Task<(byte[] FileContent, string FileName)?> GetResumeFileAsync(Guid resumeId, Guid currentUserId);
}
