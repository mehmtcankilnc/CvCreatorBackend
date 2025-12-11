using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface IResumeService
{
    Task<byte[]> CreateResumePdfAsync(ResumeFormValuesModel model, string templateName);
    Task SaveResume(byte[] fileContent, string userId, string fileName);
    Task<List<Resume>> GetResumesAsync(Guid id, string? searchText, int? number);
    Task<string> GetResumeByIdAsync(Guid resumeId);
}
