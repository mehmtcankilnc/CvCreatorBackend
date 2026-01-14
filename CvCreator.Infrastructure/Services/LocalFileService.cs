using CvCreator.Application.Contracts;
using Microsoft.AspNetCore.Hosting;

namespace CvCreator.Infrastructure.Services;

public class LocalFileService(IWebHostEnvironment env) : IFileService
{
    private readonly string _basePath = Path.Combine(env.ContentRootPath, "Uploads");

    public async Task<string> SaveFileAsync(string folderName, string fileName, byte[] content)
    {
        var folderPath = Path.Combine(_basePath, folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var filePath = Path.Combine(folderPath, fileName);
        await File.WriteAllBytesAsync(filePath, content);
        return Path.Combine(folderName, fileName);
    }

    public async Task<byte[]> GetFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Dosya Bulunamadı: ", filePath);

        return await File.ReadAllBytesAsync(fullPath);
    }

    public void DeleteFile(string path)
    {
        var fullPath = Path.Combine(_basePath, path);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
