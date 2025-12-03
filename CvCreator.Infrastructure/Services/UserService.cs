using CvCreator.Application.Contracts;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace CvCreator.Infrastructure.Services;

public class UserService(
    IConfiguration configuration, AppDbContext appDbContext) : IUserService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly IConfiguration _configuration = configuration;
    private static readonly HttpClient httpClient = new();

    public async Task DeleteRelatedData(Guid userId)
    {
        var resumesToDelete = _appDbContext.Resumes.Where(r => r.UserId == userId);
        _appDbContext.Resumes.RemoveRange(resumesToDelete);

        var lettersToDelete = _appDbContext.CoverLetters.Where(c => c.UserId == userId);
        _appDbContext.CoverLetters.RemoveRange(lettersToDelete);

        await _appDbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteUserFromSupabase(string userId)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseSecretKey = _configuration["Supabase:SecretKey"];

        var requestUrl = $"{supabaseUrl}/auth/v1/admin/users/{userId}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);

        request.Headers.Add("apikey", supabaseSecretKey);

        Console.WriteLine(userId);
        Console.WriteLine("@@");
        Console.WriteLine(request);

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var errorContent = await response.Content.ReadAsStringAsync();

        return false;
    }
}
