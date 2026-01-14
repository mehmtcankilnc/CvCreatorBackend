using AutoMapper;
using AutoMapper.QueryableExtensions;
using CvCreator.Application.Contracts;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CvCreator.Infrastructure.Services;

public class CoverLetterService
    (AppDbContext appDbContext, ITemplateService templateService,
    IPdfService pdfService,IMapper mapper, IFileService fileService) : ICoverLetterService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly ITemplateService _templateService = templateService;
    private readonly IPdfService _pdfService = pdfService;
    private readonly IMapper _mapper = mapper;
    private readonly IFileService _fileService = fileService;

    private const string FOLDER_NAME = "CoverLetters";

    public async Task<byte[]> CreateCoverLetterPdfAsync(CoverLetterFormValuesModel model)
    {
        string finalHtml = await _templateService.RenderTemplateAsync("coverletter", model);

        byte[] pdfBytes = await _pdfService.GenerateFromHtmlAsync(finalHtml);

        return pdfBytes;
    }

    public async Task<byte[]> GenerateCoverLetterAsync(CoverLetterFormValuesModel model, Guid? userId)
    {
        byte[] pdfBytes = await CreateCoverLetterPdfAsync(model);

        if (userId.HasValue)
        {
            await SaveCoverLetterAsync(pdfBytes, userId.Value.ToString(), model);
        }

        return pdfBytes;
    }

    public async Task SaveCoverLetterAsync(byte[] fileContent, string userId, CoverLetterFormValuesModel model)
    {
        var coverLetterId = Guid.NewGuid();
        var fileName = $"{coverLetterId}.pdf";

        var storagePath = await _fileService.SaveFileAsync(FOLDER_NAME, fileName, fileContent);

        var newCoverLetter = new CoverLetter
        {
            Id = coverLetterId,
            FileName = model.SenderInfo.FullName,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CoverLetterFormValues = model,
            AppUserId = Guid.Parse(userId),
        };

        _appDbContext.CoverLetters.Add(newCoverLetter);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<List<FileResponseDto>> GetCoverLettersAsync(Guid id, string? searchText, int? limit)
    {
        var finalSearchText = searchText ?? string.Empty;
        var query = _appDbContext.CoverLetters.AsNoTracking()
            .Where(coverletter => coverletter.AppUserId == id);

        if (!string.IsNullOrEmpty(searchText))
        {
            searchText = finalSearchText.Trim().ToLower();

            query = query.Where(coverletter =>
                EF.Functions.Like(coverletter.FileName.ToLower(), $"%{searchText}%"));
        }

        query = query.OrderByDescending(r => r.UpdatedAt);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query
            .ProjectTo<FileResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<CoverLetterResponseDto?> FindCoverLetterByIdAsync(Guid coverLetterId)
    {
        var entity = await _appDbContext.CoverLetters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == coverLetterId);

        return _mapper.Map<CoverLetterResponseDto?>(entity);
    }

    public async Task DeleteCoverLetterAsync(CoverLetterResponseDto coverLetter)
    {
        var entity = await _appDbContext.CoverLetters
            .FirstOrDefaultAsync(c => c.Id == coverLetter.Id);

        if (entity == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(entity.StoragePath))
        {
            _fileService.DeleteFile(entity.StoragePath);
        }

        _appDbContext.CoverLetters.Remove(entity);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task UpdateCoverLetter(byte[] fileContent, Guid coverLetterId, CoverLetterFormValuesModel model)
    {
        var entity = await _appDbContext.CoverLetters
            .FirstOrDefaultAsync(c => c.Id == coverLetterId);

        if (entity == null) return;

        if (fileContent != null && fileContent.Length > 0)
        {
            if (!string.IsNullOrEmpty(entity.StoragePath))
            {
                _fileService.DeleteFile(entity.StoragePath);
            }

            string storageFileName = $"{entity.Id}.pdf";

            string newStoragePath = await _fileService.SaveFileAsync(FOLDER_NAME, storageFileName, fileContent);

            entity.StoragePath = newStoragePath;
        }

        entity.CoverLetterFormValues = model;
        entity.FileName = model.SenderInfo.FullName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _appDbContext.SaveChangesAsync();
    }

    public async Task<(byte[] FileContent, string FileName)?> GetCoverLetterFileAsync(Guid coverLetterId, Guid currentUserId)
    {
        var entity = await _appDbContext.CoverLetters.FindAsync(coverLetterId);

        if (entity == null) return null;

        if (entity.AppUserId != currentUserId)
        {
            return null;
        }

        try
        {
            var fileBytes = await _fileService.GetFileAsync(entity.StoragePath);
            return (fileBytes, entity.FileName);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
