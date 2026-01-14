namespace CvCreator.Application.Contracts;

public interface IPdfService
{
    Task<byte[]> GenerateFromHtmlAsync(string html);
}
