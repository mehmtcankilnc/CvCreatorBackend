using CvCreator.Application.Contracts;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;
using CvCreator.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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

    public async Task SaveCoverLetter(byte[] fileContent, string userId, CoverLetterFormValuesModel model)
    {
        var coverLetterId = Guid.NewGuid();
        var userIdAsGuid = Guid.Parse(userId);

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "coverletters";
        var storagePath = $"public/{coverLetterId}";

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

    public async Task<List<CoverLetter>> GetCoverLettersAsync(Guid id, string? searchText, int? number)
    {
        var finalSearchText = searchText ?? string.Empty;
        var query = _appDbContext.CoverLetters
            .Where(coverletter => coverletter.UserId == id);

        if (!string.IsNullOrEmpty(searchText))
        {
            searchText = finalSearchText.Trim().ToLower();

            query = query.Where(coverletter =>
                EF.Functions.Like(coverletter.FileName.ToLower(), $"%{searchText}%"));
        }

        if (number.HasValue)
        {
            query = query.Take(number.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<string> CreateCoverLetterSignedUrlByIdAsync(Guid coverLetterId)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "coverletters";
        var storagePath = $"public/{coverLetterId}";

        var requestUrl = $"{supabaseUrl}/storage/v1/object/sign/{bucketName}/{storagePath}";
        var payload = new { expiresIn = 60 };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

        request.Content = JsonContent.Create(payload);

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Supabase API Hatası: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SupabaseSignedUrlResponse>(responseContent);

        if (result.SignedUrl.StartsWith("http"))
        {
            return result.SignedUrl;
        }
        else
        {
            return $"{supabaseUrl.TrimEnd('/')}/storage/v1{result.SignedUrl}";
        }
    }

    public async Task<CoverLetter?> FindCoverLetterByIdAsync(Guid coverLetterId)
    {
        return await _appDbContext.CoverLetters
            .FirstOrDefaultAsync(c => c.Id == coverLetterId);
    }

    public async Task<FileResponseDto> DownloadCoverLetterAsync(Guid coverLetterId)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "coverletters";
        var storagePath = $"public/{coverLetterId}";

        var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        
        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Download Failed: {response.StatusCode} - {errorContent}");
        }

        var stream = await response.Content.ReadAsStreamAsync();

        long? length = response.Content.Headers.ContentLength;

        return new FileResponseDto
        {
            Stream = stream,
            ContentType = "application/pdf",
            FileLength = length
        };
    }

    public async Task DeleteCoverLetterAsync(CoverLetter coverLetter)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "coverletters";
        var storagePath = $"public/{coverLetter.Id}";

        var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);

        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            throw new Exception($"Delete failed: {response.StatusCode} - {errorContent}");
        }

        _appDbContext.CoverLetters.Remove(coverLetter);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task UpdateCoverLetter(byte[] fileContent, string userId, CoverLetter coverLetter)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "coverletters";
        var storagePath = $"public/{coverLetter.Id}";

        var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);

        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");
        request.Headers.Add("x-upsert", "true");

        using var content = new ByteArrayContent(fileContent);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        request.Content = content;

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            throw new Exception($"Update failed: {response.StatusCode} - {errorContent}");
        }

        if (coverLetter.StoragePath != storagePath)
        {
            coverLetter.StoragePath = storagePath;
        }

        await _appDbContext.SaveChangesAsync();
    }
}
