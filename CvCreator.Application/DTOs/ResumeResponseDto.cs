using CvCreator.Domain.Models;

namespace CvCreator.Application.DTOs;

public class ResumeResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ResumeFormValuesModel ResumeFormValues { get; set; }
    public Guid UserId { get; set; }
}
