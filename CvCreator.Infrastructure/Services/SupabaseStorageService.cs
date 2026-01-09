using CvCreator.Application.Contracts;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CvCreator.Infrastructure.Services;

public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public SupabaseStorageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _baseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
    }

    public async Task UploadFileAsync(string storagePath, byte[] fileContent, string bucketName)
    {
        using var content = new ByteArrayContent(fileContent);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var requestUrl = $"object/{bucketName}/{storagePath}";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateFileAsync(string storagePath, byte[] fileContent, string bucketName)
    {
        var requestUrl = $"object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Headers.Add("x-upsert", "true");

        using var content = new ByteArrayContent(fileContent);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteFileAsync(string storagePath, string bucketName)
    {
        var requestUrl = $"object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> CreateSignedUrlAsync(string storagePath, string bucketName) 
    {
        var requestUrl = $"object/sign/{bucketName}/{storagePath}";
        var payload = new { expiresIn = 60 };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SupabaseSignedUrlResponse>(responseContent);

        if (result.SignedUrl.StartsWith("http"))
        {
            return result.SignedUrl;
        }
        else
        {
            return $"{_baseUrl}/{result.SignedUrl}";
        }
    }

    public async Task<HttpResponseMessage> DownloadFileAsync(string storagePath, string bucketName)
    {
        var requestUrl = $"object/{bucketName}/{storagePath}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        return response;
    }
}
