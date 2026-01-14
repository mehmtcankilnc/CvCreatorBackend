namespace CvCreator.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
    public bool IsGuest { get; set; } = false;

    public ICollection<Resume> Resumes { get; set; } = [];
    public ICollection<CoverLetter> CoverLetters { get; set; } = [];
}
