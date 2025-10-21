using CvCreator.Application.Contracts;
using CvCreator.Domain.Models;

namespace CvCreator.Application.Services;

public class ResumeService(ITemplateService templateService, IPdfService pdfService) : IResumeService
{
    private readonly ITemplateService _templateService = templateService;
    private readonly IPdfService _pdfService = pdfService;

    public async Task<byte[]> CreateResumePdfAsync(ResumeFormValuesModel model, string templateName)
    {
        string finalHtml = await _templateService.RenderTemplateAsync(templateName, model);

        byte[] pdfBytes = await _pdfService.GenerateFromHtmlAsync(finalHtml);

        return pdfBytes;
    }
}
