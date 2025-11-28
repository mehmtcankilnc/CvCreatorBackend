using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface ICoverLetterService
{
    Task<byte[]> CreateCoverLetterPdfAsync(CoverLetterFormValuesModel model);
    //Task SaveCoverLetter(string fileName, byte[] fileContent, string userId);
}
