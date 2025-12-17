namespace CvCreator.Application.DTOs;

public class FileResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
