using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface IResumeService
{
    Task<byte[]> CreateResumePdfAsync(ResumeFormValuesModel model, string templateName);
    Task<byte[]> GenerateResumeAsync(ResumeFormValuesModel model, string templateName, Guid? userId);
    Task SaveResume(byte[] fileContent, string userId, ResumeFormValuesModel model);
    Task<List<Resume>> GetResumesAsync(Guid id, string? searchText, int? limit);
    Task<Resume?> FindResumeByIdAsync(Guid resumeId);
    Task<string> CreateResumeSignedUrlByIdAsync(Guid resumeId);
    Task<PdfResponseDto> DownloadResumeAsync(Guid resumeId);
    Task DeleteResumeAsync(Resume resume);
    Task UpdateResume(byte[] fileContent, string userId, Resume resume);
}
