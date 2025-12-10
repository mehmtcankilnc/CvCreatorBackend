using CvCreator.Application.Contracts;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace CvCreator.Infrastructure.Services;

public class ResumeService
    (AppDbContext appDbContext, ITemplateService templateService,
    IPdfService pdfService, IConfiguration configuration) : IResumeService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly ITemplateService _templateService = templateService;
    private readonly IPdfService _pdfService = pdfService;
    private readonly IConfiguration _configuration = configuration;
    private static readonly HttpClient httpClient = new();

    public async Task<byte[]> CreateResumePdfAsync(ResumeFormValuesModel model, string templateName)
    {
        string finalHtml = await _templateService.RenderTemplateAsync(templateName, model);

        byte[] pdfBytes = await _pdfService.GenerateFromHtmlAsync(finalHtml);

        return pdfBytes;
    }

    public async Task SaveResume(string fileName, byte[] fileContent, string userId)
    {
        var resumeId = Guid.NewGuid();
        var userIdAsGuid = Guid.Parse(userId);

        var nameOnly = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        var safeName = nameOnly.ToLowerInvariant();

        safeName = safeName.Replace("ğ", "g")
                           .Replace("ü", "u")
                           .Replace("ş", "s")
                           .Replace("ı", "i")
                           .Replace("ö", "o")
                           .Replace("ç", "c");

        safeName = safeName.Replace(" ", "-");

        safeName = Regex.Replace(safeName, @"[^a-z0-9-]", "");

        safeName = Regex.Replace(safeName, @"-{2,}", "-");

        safeName = safeName.Trim('-');

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "resume";
        }

        var safeFileName = $"{safeName}{extension.ToLowerInvariant()}";

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "resumes";
        var storagePath = $"public/{resumeId}-{safeFileName}";

        var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

        using var content = new ByteArrayContent(fileContent);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        request.Content = content;

        var response = await httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var newResume = new Resume
        {
            Id = resumeId,
            FileName = safeFileName,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = userIdAsGuid,
        };

        _appDbContext.Resumes.Add(newResume);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<List<Resume>> GetResumesAsync(Guid id, string? searchText, int? number)
    {
        var finalSearchText = searchText ?? string.Empty;
        var query = _appDbContext.Resumes.Where(resume => resume.UserId == id);

        if (!string.IsNullOrEmpty(finalSearchText))
        {
            searchText = finalSearchText.Trim().ToLower();

            query = query.Where(resume =>
                EF.Functions.Like(resume.FileName.ToLower(), $"%{searchText}%"));
        }

        if (number.HasValue)
        {
            query = query.Take(number.Value);
        }

        return await query.ToListAsync();
    }
}
