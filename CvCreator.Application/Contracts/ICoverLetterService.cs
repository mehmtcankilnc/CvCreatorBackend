using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface ICoverLetterService
{
    Task<byte[]> CreateCoverLetterPdfAsync(CoverLetterFormValuesModel model);
    Task SaveCoverLetter(string fileName, byte[] fileContent, string userId);
    Task<List<CoverLetter>> GetCoverLettersAsync(Guid id, string? searchText, int? number);
}
