using CvCreator.Application.Contracts;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace CvCreator.Infrastructure.Services;

public class ResumeService
    (AppDbContext appDbContext, ITemplateService templateService, 
    IPdfService pdfService, IConfiguration configuration) : IResumeService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly ITemplateService _templateService = templateService;
    private readonly IPdfService _pdfService = pdfService;
    private readonly IConfiguration _configuration = configuration;
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task<byte[]> CreateResumePdfAsync(ResumeFormValuesModel model, string templateName)
    {
        string finalHtml = await _templateService.RenderTemplateAsync(templateName, model);

        byte[] pdfBytes = await _pdfService.GenerateFromHtmlAsync(finalHtml);

        return pdfBytes;
    }


    public async Task SaveResume(string fileName, byte[] fileContent, string userId)
    {
        var resumeId = Guid.NewGuid();

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:ServiceKey"];
        var bucketName = "resumes";
        var storagePath = $"public/{resumeId}-{fileName}";

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
            FileName = fileName,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = userId,
        };

        _appDbContext.Resumes.Add(newResume);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<List<Resume>> GetResumesAsync(string id)
    {
        var resumes = await _appDbContext.Resumes.Where(resume => resume.UserId == id).ToListAsync();
        return resumes;
    }
}
