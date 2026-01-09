using AutoMapper;
using AutoMapper.QueryableExtensions;
using CvCreator.Application.Contracts;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CvCreator.Infrastructure.Services;

public class ResumeService
    (AppDbContext appDbContext, ITemplateService templateService,
    IPdfService pdfService, IMapper mapper, SupabaseStorageService supabaseService) : IResumeService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly ITemplateService _templateService = templateService;
    private readonly IPdfService _pdfService = pdfService;
    private readonly IMapper _mapper = mapper;
    private readonly SupabaseStorageService _supabaseService = supabaseService;

    private const string BUCKET_NAME = "resumes";

    public async Task<byte[]> CreateResumePdfAsync(ResumeFormValuesModel model, string templateName)
    {
        string finalHtml = await _templateService.RenderTemplateAsync(templateName, model);

        byte[] pdfBytes = await _pdfService.GenerateFromHtmlAsync(finalHtml);

        return pdfBytes;
    }

    public async Task<byte[]> GenerateResumeAsync(ResumeFormValuesModel model, string templateName, Guid? userId)
    {
        byte[] pdfBytes = await CreateResumePdfAsync(model, templateName);

        if (userId.HasValue)
        {
            await SaveResume(pdfBytes, userId.Value.ToString(), model);
        }

        return pdfBytes;
    }

    public async Task SaveResume(byte[] fileContent, string userId, ResumeFormValuesModel model)
    {
        var resumeId = Guid.NewGuid();
        var userIdAsGuid = Guid.Parse(userId);

        var storagePath = $"public/{resumeId}";

        await _supabaseService.UploadFileAsync(storagePath, fileContent, BUCKET_NAME);

        var newResume = new Resume
        {
            Id = resumeId,
            FileName = model.PersonalInfo.FullName,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ResumeFormValues = model,
            UserId = userIdAsGuid,
        };

        _appDbContext.Resumes.Add(newResume);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<List<FileResponseDto>> GetResumesAsync(Guid id, string? searchText, int? limit)
    {
        var finalSearchText = searchText ?? string.Empty;
        var query = _appDbContext.Resumes.AsNoTracking()
            .Where(resume => resume.UserId == id);

        if (!string.IsNullOrEmpty(finalSearchText))
        {
            searchText = finalSearchText.Trim().ToLower();

            query = query.Where(resume =>
                EF.Functions.Like(resume.FileName.ToLower(), $"%{searchText}%"));
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

    public async Task<string> CreateResumeSignedUrlByIdAsync(Guid resumeId)
    {
        var storagePath = $"public/{resumeId}";

        return await _supabaseService.CreateSignedUrlAsync(storagePath, BUCKET_NAME);
    }

    public async Task<ResumeResponseDto?> FindResumeByIdAsync(Guid resumeId)
    {
        var entity = await _appDbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == resumeId);

        return _mapper.Map<ResumeResponseDto>(entity);
    }

    public async Task<PdfResponseDto> DownloadResumeAsync(Guid resumeId)
    {
        var storagePath = $"public/{resumeId}";

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

    public async Task DeleteResumeAsync(ResumeResponseDto resume)
    {
        var storagePath = $"public/{resume.Id}";

        await _supabaseService.DeleteFileAsync(storagePath, BUCKET_NAME);

        var entity = await _appDbContext.Resumes
            .FirstOrDefaultAsync(c => c.Id == resume.Id);
        _appDbContext.Resumes.Remove(entity!);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task UpdateResume(byte[] fileContent, Guid resumeId, ResumeFormValuesModel model)
    {
        var entity = await _appDbContext.Resumes
            .FirstOrDefaultAsync(c => c.Id == resumeId);

        var storagePath = $"public/{entity!.Id}";

        await _supabaseService.UpdateFileAsync(storagePath, fileContent, BUCKET_NAME);

        entity.ResumeFormValues = model;
        entity.FileName = model.PersonalInfo.FullName;
        entity.UpdatedAt = DateTime.UtcNow;

        if (entity!.StoragePath != storagePath)
        {
            entity.StoragePath = storagePath;
        }

        await _appDbContext.SaveChangesAsync();
    }
}

public class SupabaseSignedUrlResponse
{
    [JsonPropertyName("signedURL")]
    public string SignedUrl { get; set; }
}
