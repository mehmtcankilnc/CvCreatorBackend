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
    IPdfService pdfService,IMapper mapper, SupabaseStorageService supabaseService) : ICoverLetterService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly ITemplateService _templateService = templateService;
    private readonly IPdfService _pdfService = pdfService;
    private readonly IMapper _mapper = mapper;
    private readonly SupabaseStorageService _supabaseService = supabaseService;

    private const string BUCKET_NAME = "coverletters";

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
            await SaveCoverLetter(pdfBytes, userId.Value.ToString(), model);
        }

        return pdfBytes;
    }

    public async Task SaveCoverLetter(byte[] fileContent, string userId, CoverLetterFormValuesModel model)
    {
        var coverLetterId = Guid.NewGuid();
        var userIdAsGuid = Guid.Parse(userId);

        var storagePath = $"public/{coverLetterId}";

        await _supabaseService.UploadFileAsync(storagePath, fileContent, BUCKET_NAME);

        var newCoverLetter = new CoverLetter
        {
            Id = coverLetterId,
            FileName = model.SenderInfo.FullName,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CoverLetterFormValues = model,
            UserId = userIdAsGuid,
        };

        _appDbContext.CoverLetters.Add(newCoverLetter);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<List<FileResponseDto>> GetCoverLettersAsync(Guid id, string? searchText, int? limit)
    {
        var finalSearchText = searchText ?? string.Empty;
        var query = _appDbContext.CoverLetters.AsNoTracking()
            .Where(coverletter => coverletter.UserId == id);

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

    public async Task<string> CreateCoverLetterSignedUrlByIdAsync(Guid coverLetterId)
    {
        var storagePath = $"public/{coverLetterId}";

        return await _supabaseService.CreateSignedUrlAsync(storagePath, BUCKET_NAME);
    }

    public async Task<CoverLetterResponseDto?> FindCoverLetterByIdAsync(Guid coverLetterId)
    {
        var entity = await _appDbContext.CoverLetters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == coverLetterId);

        return _mapper.Map<CoverLetterResponseDto?>(entity);
    }

    public async Task<PdfResponseDto> DownloadCoverLetterAsync(Guid coverLetterId)
    {
        var storagePath = $"public/{coverLetterId}";

        var response = await _supabaseService.DownloadFileAsync(storagePath, BUCKET_NAME);

        var stream = await response.Content.ReadAsStreamAsync();

        long? length = response.Content.Headers.ContentLength;

        return new PdfResponseDto
        {
            Stream = stream,
            ContentType = "application/pdf",
            FileLength = length
        };
    }

    public async Task DeleteCoverLetterAsync(CoverLetterResponseDto coverLetter)
    {
        var storagePath = $"public/{coverLetter.Id}";

        await _supabaseService.DeleteFileAsync(storagePath, BUCKET_NAME);

        var entity = await _appDbContext.CoverLetters
            .FirstOrDefaultAsync(c => c.Id == coverLetter.Id);
        _appDbContext.CoverLetters.Remove(entity!);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task UpdateCoverLetter(byte[] fileContent, Guid coverLetterId, CoverLetterFormValuesModel model)
    {
        var entity = await _appDbContext.CoverLetters
            .FirstOrDefaultAsync(c => c.Id == coverLetterId);

        var storagePath = $"public/{entity!.Id}";

        await _supabaseService.UpdateFileAsync(storagePath, fileContent, BUCKET_NAME);

        entity.CoverLetterFormValues = model;
        entity.FileName = model.SenderInfo.FullName;
        entity.UpdatedAt = DateTime.UtcNow;

        if (entity!.StoragePath != storagePath)
        {
            entity.StoragePath = storagePath;
        }

        await _appDbContext.SaveChangesAsync();
    }
}
