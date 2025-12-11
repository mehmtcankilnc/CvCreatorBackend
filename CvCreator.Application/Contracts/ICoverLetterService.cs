using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface ICoverLetterService
{
    Task<byte[]> CreateCoverLetterPdfAsync(CoverLetterFormValuesModel model);
    Task SaveCoverLetter(byte[] fileContent, string userId, string fileName);
    Task<List<CoverLetter>> GetCoverLettersAsync(Guid id, string? searchText, int? number);
    Task<string> GetCoverLetterByIdAsync(Guid coverLetterId);
}
