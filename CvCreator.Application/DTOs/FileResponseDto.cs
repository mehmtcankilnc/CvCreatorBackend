namespace CvCreator.Application.DTOs;

public class FileResponseDto
{
    public Stream Stream { get; set; }
    public string ContentType { get; set; }
    public long? FileLength { get; set; }
}
