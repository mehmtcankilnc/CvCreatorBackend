namespace CvCreator.Application.Contracts;

public interface ISupabaseStorageService
{
    public Task UploadFileAsync(string storagePath, byte[] fileContent, string bucketName);
    public Task UpdateFileAsync(string storagePath, byte[] fileContent, string bucketName);
    public Task DeleteFileAsync(string storagePath, string bucketName);
    public Task<string> CreateSignedUrlAsync(string storagePath, string bucketName);
    public Task<HttpResponseMessage> DownloadFileAsync(string storagePath, string bucketName);
}
