using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface IResumeService
{
    Task<byte[]> CreateResumePdfAsync(ResumeFormValuesModel model, string templateName);
    Task SaveResume(string fileName, byte[] fileContent, string userId);
    Task<List<Resume>> GetResumesAsync(string id);
}
