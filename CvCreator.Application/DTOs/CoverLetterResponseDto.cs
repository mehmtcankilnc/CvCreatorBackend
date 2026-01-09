using CvCreator.Domain.Models;

namespace CvCreator.Application.DTOs;

public class CoverLetterResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public CoverLetterFormValuesModel CoverLetterFormValues { get; set; }
    public Guid UserId { get; set; }
}
