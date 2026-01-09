using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;

namespace CvCreator.Application.Contracts;

public interface ICoverLetterService
{
    Task<byte[]> CreateCoverLetterPdfAsync(CoverLetterFormValuesModel model);
    Task<byte[]> GenerateCoverLetterAsync(CoverLetterFormValuesModel model, Guid? userId);
    Task SaveCoverLetter(byte[] fileContent, string userId, CoverLetterFormValuesModel model);
    Task<List<FileResponseDto>> GetCoverLettersAsync(Guid id, string? searchText, int? limit);
    Task<CoverLetterResponseDto?> FindCoverLetterByIdAsync(Guid coverLetterId);
    Task<string> CreateCoverLetterSignedUrlByIdAsync(Guid coverLetterId);
    Task<PdfResponseDto> DownloadCoverLetterAsync(Guid coverLetterId);
    Task DeleteCoverLetterAsync(CoverLetterResponseDto coverLetter);
    Task UpdateCoverLetter(byte[] fileContent, Guid coverLetterId, CoverLetterFormValuesModel model);
}
