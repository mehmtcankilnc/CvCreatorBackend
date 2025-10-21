using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CvCreator.Domain.Models;

public class ResumeFormValuesModel
{
    public PersonalInfo PersonalInfo { get; set; }
    public SummaryInfo? SummaryInfo { get; set; }
    public List<EducationInfo>? EducationsInfo { get; set; }
    public List<ExperienceInfo>? ExperiencesInfo { get; set; }
    public List<CertificateInfo>? CertificatesInfo { get; set; }
    public List<SkillInfo>? SkillsInfo { get; set; }
    public List<LanguageInfo>? LanguagesInfo { get; set; }
    public List<ReferenceInfo>? ReferencesInfo { get; set; }
}

public class PersonalInfo
{
    public string FullName { get; set; }
    public string? JobTitle { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
}

public class SummaryInfo
{
    public string Text { get; set; }
}

public class EducationInfo
{
    public bool? IsCurrent { get; set; }
    public string Title { get; set; }
    public string StartDate { get; set; }
    public string? EndDate { get; set; }
    public string Institute { get; set; }
    public string? Gpa { get; set; }
}

public class ExperienceInfo
{
    public bool? IsCurrent { get; set; }
    public string Title { get; set; }
    public string StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Company { get; set; }
    public string? Text { get; set; }
}

public class CertificateInfo
{
    public string Title { get; set; }
    public string? Date { get; set; }
    public string? Issuer { get; set; }
    public string? Link { get; set; }
}

public class SkillInfo
{
    public string Title { get; set; }
    public string? Scale { get; set; }
}

public class LanguageInfo
{
    public string Title { get; set; }
    public string? Scale { get; set; }
}

public class ReferenceInfo
{
    public string FullName { get; set; }
    public string Contact { get; set; }
}