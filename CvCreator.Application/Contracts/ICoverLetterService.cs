using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface ICoverLetterService
{
    Task<byte[]> CreateCoverLetterPdfAsync(CoverLetterFormValuesModel model);
    Task SaveCoverLetter(byte[] fileContent, string userId, CoverLetterFormValuesModel model);
    Task<List<CoverLetter>> GetCoverLettersAsync(Guid id, string? searchText, int? number);
    Task<CoverLetter?> FindCoverLetterByIdAsync(Guid coverLetterId);
    Task<string> CreateCoverLetterSignedUrlByIdAsync(Guid coverLetterId);
    Task<FileResponseDto> DownloadCoverLetterAsync(Guid coverLetterId);
    Task DeleteCoverLetterAsync(CoverLetter coverLetter);
    Task UpdateCoverLetter(byte[] fileContent, string userId, CoverLetter coverLetter);
}
