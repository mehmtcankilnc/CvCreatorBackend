using CvCreator.Application.Contracts;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public async Task SaveResume(byte[] fileContent, string userId, ResumeFormValuesModel model)
    {
        var resumeId = Guid.NewGuid();
        var userIdAsGuid = Guid.Parse(userId);

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "resumes";
        var storagePath = $"public/{resumeId}";

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

    public async Task<string> CreateResumeSignedUrlByIdAsync(Guid resumeId)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "resumes";
        var storagePath = $"public/{resumeId}";

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

    public async Task<Resume?> FindResumeByIdAsync(Guid resumeId)
    {
        return await _appDbContext.Resumes
            .FirstOrDefaultAsync(c => c.Id == resumeId);
    }

    public async Task<FileResponseDto> DownloadResumeAsync(Guid resumeId)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "resumes";
        var storagePath = $"public/{resumeId}";

        var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"{response.StatusCode} - {errorContent}");
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

    public async Task DeleteResumeAsync(Resume resume)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "resumes";
        var storagePath = $"public/{resume.Id}";

        var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);

        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            throw new Exception($"Delete failed: {response.StatusCode} - {errorContent}");
        }

        _appDbContext.Resumes.Remove(resume);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task UpdateResume(byte[] fileContent, string userId, Resume resume)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "resumes";
        var storagePath = $"public/{resume.Id}";

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

        if (resume.StoragePath != storagePath)
        {
            resume.StoragePath = storagePath;
        }

        await _appDbContext.SaveChangesAsync();
    }
}

public class SupabaseSignedUrlResponse
{
    [JsonPropertyName("signedURL")]
    public string SignedUrl { get; set; }
}
