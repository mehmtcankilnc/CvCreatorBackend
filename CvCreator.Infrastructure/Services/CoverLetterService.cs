using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using CvCreator.Application.Contracts;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CvCreator.Infrastructure.Services;

public class CoverLetterService
    (AppDbContext appDbContext, ITemplateService templateService,
    IPdfService pdfService, IConfiguration configuration) : ICoverLetterService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly ITemplateService _templateService = templateService;
    private readonly IPdfService _pdfService = pdfService;
    private readonly IConfiguration _configuration = configuration;
    private static readonly HttpClient httpClient = new();

    public async Task<byte[]> CreateCoverLetterPdfAsync(CoverLetterFormValuesModel model)
    {
        string finalHtml = await _templateService.RenderTemplateAsync("coverletter", model);

        byte[] pdfBytes = await _pdfService.GenerateFromHtmlAsync(finalHtml);

        return pdfBytes;
    }

    public async Task SaveCoverLetter(string fileName, byte[] fileContent, string userId)
    {
        var coverLetterId = Guid.NewGuid();
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
            safeName = "coverletter";
        }

        var safeFileName = $"{safeName}{extension.ToLowerInvariant()}";

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "coverletters";
        var storagePath = $"public/{coverLetterId}-{safeFileName}";

        var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

        using var content = new ByteArrayContent(fileContent);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        request.Content = content;

        var response = await httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var newCoverLetter = new CoverLetter
        {
            Id = coverLetterId,
            FileName = safeFileName,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = userIdAsGuid,
        };

        _appDbContext.CoverLetters.Add(newCoverLetter);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<List<CoverLetter>> GetCoverLettersAsync(Guid id, string? searchText)
    {
        var query = _appDbContext.CoverLetters
            .Where(coverletter => coverletter.UserId == id);

        if (!string.IsNullOrEmpty(searchText))
        {
            searchText = searchText.ToLower();

            query = query.Where(coverletter =>
                EF.Functions.Like(coverletter.FileName.ToLower(), $"%{searchText}%"));
        }

        return await query.ToListAsync();
    }
}
