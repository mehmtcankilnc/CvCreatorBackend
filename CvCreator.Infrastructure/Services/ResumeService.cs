using AutoMapper;
using AutoMapper.QueryableExtensions;
using CvCreator.Application.Contracts;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CvCreator.Infrastructure.Services;

public class ResumeService
    (AppDbContext appDbContext, ITemplateService templateService,
    IPdfService pdfService, IMapper mapper, IFileService fileService) : IResumeService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly ITemplateService _templateService = templateService;
    private readonly IPdfService _pdfService = pdfService;
    private readonly IMapper _mapper = mapper;
    private readonly IFileService _fileService = fileService;

    private const string FOLDER_NAME = "Resumes";

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
            await SaveResumeAsync(pdfBytes, userId.Value.ToString(), model);
        }

        return pdfBytes;
    }

    public async Task SaveResumeAsync(byte[] fileContent, string userId, ResumeFormValuesModel model)
    {
        var resumeId = Guid.NewGuid();
        var fileName = $"{resumeId}.pdf";

        var storagePath = await _fileService.SaveFileAsync(FOLDER_NAME, fileName, fileContent);

        var newResume = new Resume
        {
            Id = resumeId,
            FileName = model.PersonalInfo.FullName,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ResumeFormValues = model,
            AppUserId = Guid.Parse(userId),
        };

        _appDbContext.Resumes.Add(newResume);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<List<FileResponseDto>> GetResumesAsync(Guid id, string? searchText, int? limit)
    {
        var finalSearchText = searchText ?? string.Empty;
        var query = _appDbContext.Resumes.AsNoTracking()
            .Where(resume => resume.AppUserId == id);

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

    public async Task<ResumeResponseDto?> FindResumeByIdAsync(Guid resumeId)
    {
        var entity = await _appDbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == resumeId);

        return _mapper.Map<ResumeResponseDto>(entity);
    }

    public async Task DeleteResumeAsync(ResumeResponseDto resume)
    {
        var entity = await _appDbContext.Resumes
            .FirstOrDefaultAsync(c => c.Id == resume.Id);

        if (entity == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(entity.StoragePath))
        {
            _fileService.DeleteFile(entity.StoragePath);
        }

        _appDbContext.Resumes.Remove(entity);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task UpdateResume(byte[] fileContent, Guid resumeId, ResumeFormValuesModel model)
    {
        var entity = await _appDbContext.Resumes
            .FirstOrDefaultAsync(c => c.Id == resumeId);

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

        entity.ResumeFormValues = model;
        entity.FileName = model.PersonalInfo.FullName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _appDbContext.SaveChangesAsync();
    }

    public async Task<(byte[] FileContent, string FileName)?> GetResumeFileAsync(Guid resumeId, Guid currentUserId)
    {
        var entity = await _appDbContext.Resumes.FindAsync(resumeId);

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
