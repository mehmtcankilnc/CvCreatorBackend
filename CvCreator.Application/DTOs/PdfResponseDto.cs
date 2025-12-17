namespace CvCreator.Application.DTOs;

public class PdfResponseDto
{
    public Stream Stream { get; set; }
    public string ContentType { get; set; }
    public long? FileLength { get; set; }
}
