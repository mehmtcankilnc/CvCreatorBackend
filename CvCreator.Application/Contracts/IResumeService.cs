using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface IResumeService
{
    Task<byte[]> CreateResumePdfAsync(ResumeFormValuesModel model, string templateName);
}
