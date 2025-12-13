using CvCreator.Domain.Models;

namespace CvCreator.Domain.Entities;

public class Resume
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string StoragePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ResumeFormValuesModel ResumeFormValues { get; set; }
    public Guid UserId { get; set; }
}
