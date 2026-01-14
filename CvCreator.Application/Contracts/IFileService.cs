namespace CvCreator.Application.Contracts;

public interface IFileService
{
    Task<string> SaveFileAsync(string folderName, string fileName, byte[] content);
    Task<byte[]> GetFileAsync(string filePath);
    void DeleteFile(string path);
}
