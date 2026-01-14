using CvCreator.Domain.Models;

namespace CvCreator.Domain.Entities;

public class CoverLetter
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string StoragePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public CoverLetterFormValuesModel CoverLetterFormValues { get; set; }
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
}
