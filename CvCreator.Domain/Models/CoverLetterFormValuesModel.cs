namespace CvCreator.Domain.Models;

public class CoverLetterFormValuesModel
{
    public SenderInfo SenderInfo { get; set; }
    public RecipientInfo RecipientInfo { get; set; }
    public MetaInfo MetaInfo { get; set; }
    public Content Content { get; set; }
}

public class SenderInfo
{
    public string FullName { get; set; }
    public string? JobTitle { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
}

public class RecipientInfo
{
    public string CompanyName { get; set; }
    public string HiringManagerName { get; set; }
}

public class MetaInfo
{
    public string Subject { get; set; }
    public string SentDate { get; set; }
}

public class Content
{
    public string Salutation { get; set; }
    public string Introduction { get; set; }
    public string Body { get; set; }
    public string Conclusion { get; set; }
    public string SignOff { get; set; }
}